using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SqlSyntaxHighlighting.NaturalTextTaggers;

namespace SqlSyntaxHighlighting
{
	[Export(typeof(IClassifierProvider))]
	[ContentType("code")]
	internal class SqlClassifierProvider : IClassifierProvider
	{
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("sql")]
		internal ClassificationTypeDefinition SqlClassificationType;

		[Import]
		internal IClassificationTypeRegistryService ClassificationRegistry;

		[Import]
		internal IBufferTagAggregatorFactoryService TagAggregatorFactory;

		public IClassifier GetClassifier(ITextBuffer buffer)
		{
			var tagAggregator = TagAggregatorFactory.CreateTagAggregator<NaturalTextTag>(buffer);
			return new SqlClassifier(tagAggregator, ClassificationRegistry);
		}
	}
}
