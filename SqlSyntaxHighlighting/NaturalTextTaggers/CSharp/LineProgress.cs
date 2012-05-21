using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace SqlSyntaxHighlighting.NaturalTextTaggers.CSharp
{
    class LineProgress
    {
        private readonly ITextSnapshotLine snapshotLine;
        private readonly List<SnapshotSpan> naturalTextSpans;
		private readonly string lineText;
        private int naturalTextStart = -1;
		private int linePosition;

        public State State { get; set; }

        public LineProgress(ITextSnapshotLine line, State state, List<SnapshotSpan> naturalTextSpans)
        {
            snapshotLine = line;
            lineText = line.GetText();
            linePosition = 0;
            this.naturalTextSpans = naturalTextSpans;

            State = state;
        }

        public bool EndOfLine
        {
            get { return linePosition >= snapshotLine.Length; }
        }

        public char Char()
        {
            return lineText[linePosition];
        }

        public char NextChar()
        {
            return linePosition < snapshotLine.Length - 1 ?
                lineText[linePosition + 1] :
                (char)0;
        }

        public char NextNextChar()
        {
            return linePosition < snapshotLine.Length - 2 ?
                lineText[linePosition + 2] :
                (char)0;
        }

        public void Advance(int count = 1)
        {
            linePosition += count;
        }

        public void AdvanceToEndOfLine()
        {
            linePosition = snapshotLine.Length;
        }

        public void StartNaturalText()
        {
            Debug.Assert(naturalTextStart == -1, "Called StartNaturalText() twice without call to EndNaturalText()?");
            naturalTextStart = linePosition;
        }

        public void EndNaturalText()
        {
            Debug.Assert(naturalTextStart != -1, "Called EndNaturalText() without StartNaturalText()?");
            if (naturalTextSpans != null && linePosition > naturalTextStart)
            {
                naturalTextSpans.Add(new SnapshotSpan(snapshotLine.Start + naturalTextStart, linePosition - naturalTextStart));
            }
            naturalTextStart = -1;
        }
    }
}