using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SqlSyntaxHighlighting.NaturalTextTaggers.CSharp
{
	/// <summary>
	/// Due to issues with the built-in C# classifier, we write our own NaturalTextTagger that looks for 
	/// comments (single, multi-line, and doc comment) and strings (single and multi-line) and tags them
	/// with NaturalTextTag.
	/// </summary>
	internal class CSharpCommentTextTagger : ITagger<NaturalTextTag>, IDisposable
	{
		readonly ITextBuffer buffer;
		ITextSnapshot lineCacheSnapshot;
		readonly List<State> lineCache;

		public CSharpCommentTextTagger(ITextBuffer buffer)
		{
			this.buffer = buffer;

			// Populate our cache initially.
			ITextSnapshot snapshot = this.buffer.CurrentSnapshot;
			lineCache = new List<State>(snapshot.LineCount);
			lineCache.AddRange(Enumerable.Repeat(State.Default, snapshot.LineCount));

			RescanLines(snapshot, 0, snapshot.LineCount - 1);
			lineCacheSnapshot = snapshot;

			// Listen for text changes so we can stay up-to-date
			this.buffer.Changed += OnTextBufferChanged;
		}

		public void Dispose()
		{
			buffer.Changed -= OnTextBufferChanged;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			foreach (SnapshotSpan span in spans)
			{
				// If we're called on the non-current snapshot, return nothing
				if (span.Snapshot != lineCacheSnapshot)
					yield break;

				SnapshotPoint lineStart = span.Start;
				while (lineStart < span.End)
				{
					ITextSnapshotLine line = lineStart.GetContainingLine();
					State state = line.LineNumber > 0 && lineCache[line.LineNumber - 1] == State.MultiLineString 
						? State.MultiLineString 
						: State.Default;

					List<SnapshotSpan> naturalTextSpans = new List<SnapshotSpan>();
					ScanLine(state, line, naturalTextSpans);
					foreach (SnapshotSpan naturalTextSpan in naturalTextSpans)
					{
						if (naturalTextSpan.IntersectsWith(span))
							yield return new TagSpan<NaturalTextTag>(naturalTextSpan, new NaturalTextTag());
					}

					// Advance to next line
					lineStart = line.EndIncludingLineBreak;
				}
			}
		}

		private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			ITextSnapshot snapshot = e.After;

			// First update _lineCache so its size matches snapshot.LineCount
			foreach (ITextChange change in e.Changes)
			{
				if (change.LineCountDelta > 0)
				{
					int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
					lineCache.InsertRange(line, Enumerable.Repeat(State.Default, change.LineCountDelta));
				}
				else if (change.LineCountDelta < 0)
				{
					int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
					lineCache.RemoveRange(line, -change.LineCountDelta);
				}
			}

			// Now that _lineCache is the appropriate size we can safely start rescanning.
			// If we hadn't updated _lineCache, then rescanning could walk off the edge.
			List<SnapshotSpan> changedSpans = (from change in e.Changes
			                                   let startLine = snapshot.GetLineFromPosition(change.NewPosition)
			                                   let endLine = snapshot.GetLineFromPosition(change.NewPosition)
			                                   let lastUpdatedLine = RescanLines(snapshot, startLine.LineNumber, endLine.LineNumber)
			                                   select new SnapshotSpan(startLine.Start, snapshot.GetLineFromLineNumber(lastUpdatedLine).End)).ToList();

			lineCacheSnapshot = snapshot;

			var tagsChanged = TagsChanged;
			if (tagsChanged != null)
			{
				foreach (SnapshotSpan span in changedSpans)
				{
					tagsChanged(this, new SnapshotSpanEventArgs(span));
				}
			}
		}

		// Returns last line updated (will be greater than or equal to lastDirtyLine)
		private int RescanLines(ITextSnapshot snapshot, int startLine, int lastDirtyLine)
		{
			int currentLine = startLine;
			bool updatedStateForCurrentLine = true;
			State state = currentLine > 0 && lineCache[currentLine - 1] == State.MultiLineString
				? State.MultiLineString
				: State.Default;			

			// Go until we have covered all of the dirty lines and we get to a line where our
			// new state matches the old state
			while (currentLine < lastDirtyLine || (updatedStateForCurrentLine && currentLine < snapshot.LineCount))
			{
				ITextSnapshotLine line = snapshot.GetLineFromLineNumber(currentLine);
				state = ScanLine(state, line);

				if (currentLine < snapshot.LineCount)
				{
					updatedStateForCurrentLine = (state != lineCache[currentLine]);
					lineCache[currentLine] = state;
				}

				// Advance to next line
				currentLine++;
			}

			// Last line updated
			return currentLine - 1;
		}

		private State ScanLine(State state, ITextSnapshotLine line, List<SnapshotSpan> naturalTextSpans = null)
		{
			LineProgress p = new LineProgress(line, state, naturalTextSpans);

			while (!p.EndOfLine)
			{
				if (p.State == State.Default)
					ScanDefault(p);
				else if (p.State == State.MultiLineString)
					ScanMultiLineString(p);
				else
					Debug.Fail("Invalid state at beginning of line.");
			}

			// End Of Line state must be one of these.
			Debug.Assert(p.State == State.Default || p.State == State.MultiLineString);

			return p.State;
		}

		private void ScanDefault(LineProgress p)
		{
			while (!p.EndOfLine)
			{
				if (p.Char() == '@' && p.NextChar() == '"') // multi-line string
				{
					p.Advance(2);
					p.State = State.MultiLineString;
					ScanMultiLineString(p);
				}
				else if (p.Char() == '"') // single-line string
				{
					p.Advance();
					p.State = State.String;
					ScanString(p);
				}
				else
				{
					p.Advance();
				}
			}
		}

		private void ScanString(LineProgress p)
		{
			p.StartNaturalText();

			while (!p.EndOfLine)
			{
				if (p.Char() == '\\') // escaped character. Skip over it.
				{
					p.Advance(2);
				}
				else if (p.Char() == '"') // end of string.
				{
					p.EndNaturalText();
					p.Advance();
					p.State = State.Default;

					return;
				}
				else
				{
					p.Advance();
				}
			}

			// End of line.  String wasn't closed.  Oh well.  Revert to Default state.
			p.EndNaturalText();
			p.State = State.Default;
		}

		private void ScanMultiLineString(LineProgress p)
		{
			p.StartNaturalText();

			while (!p.EndOfLine) {
				if (p.Char() == '"' && p.NextChar() == '"') // "" is allowed within multiline string
				{
					p.Advance(2);
				}
				else if (p.Char() == '"') // end of multi-line string
				{
					p.EndNaturalText();

					p.Advance();
					p.State = State.Default;
					return;
				}
				else {
					p.Advance();
				}
			}

			// End of line. Emit as human readable, but remain in MultiLineString state
			p.EndNaturalText();
			Debug.Assert(p.State == State.MultiLineString);
		}
	}
}