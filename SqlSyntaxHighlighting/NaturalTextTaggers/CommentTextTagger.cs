using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SqlSyntaxHighlighting.NaturalTextTaggers.CSharp;

namespace SqlSyntaxHighlighting.NaturalTextTaggers
{
	[Export(typeof(ITaggerProvider))]
	[ContentType("CSharp")]
	[TagType(typeof(NaturalTextTag))]
	internal class CommentTextTaggerProvider : ITaggerProvider
	{
		[Import]
		internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

		[Import]
		internal IBufferTagAggregatorFactoryService TagAggregatorFactory { get; set; }

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

			// Due to an issue with the built-in C# classifier, we avoid using it.
			if (buffer.ContentType.IsOfType("csharp"))
				return new CSharpCommentTextTagger(buffer) as ITagger<T>;

			var classifierAggregator = ClassifierAggregatorService.GetClassifier(buffer);

			return new CommentTextTagger(buffer, classifierAggregator) as ITagger<T>;
		}
	}

	internal class CommentTextTagger : ITagger<NaturalTextTag>, IDisposable
	{
		readonly ITextBuffer buffer;
		readonly IClassifier classifier;

		public CommentTextTagger(ITextBuffer buffer, IClassifier classifier)
		{
			this.buffer = buffer;
			this.classifier = classifier;

			classifier.ClassificationChanged += ClassificationChanged;
		}

		public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (classifier == null || spans == null || spans.Count == 0)
				yield break;

			foreach (var snapshotSpan in spans)
			{
				Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);
				foreach (ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
				{
					string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

					if (name.Contains("string")	&& name.Contains("xml doc tag") == false)
					{
						yield return new TagSpan<NaturalTextTag>(classificationSpan.Span, new NaturalTextTag());
					}
				}
			}
		}

		void ClassificationChanged(object sender, ClassificationChangedEventArgs e)
		{
			var temp = TagsChanged;
			if (temp != null)
				temp(this, new SnapshotSpanEventArgs(e.ChangeSpan));
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public void Dispose()
		{
			if (classifier != null)
				classifier.ClassificationChanged -= ClassificationChanged;
		}
	}
}
