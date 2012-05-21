using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using SqlSyntaxHighlighting.NaturalTextTaggers;

namespace SqlSyntaxHighlighting
{
	class SqlClassifier : IClassifier
	{
		private readonly char[] keywordPrefixCharacters = new[] { '\t', ' ', '"', '(' };
		private readonly char[] keywordPostfixCharacters = new[] { '\t', ' ', '"', ')' };
		private readonly char[] functionPrefixCharacters = new[] { '\t', ' ', '"', ',' };
		private readonly char[] functionPostfixCharacters = new[] { '\t', '(' };

		private readonly List<string> keywords = new List<string> {
		    "select", "insert", "delete", "update",
			"into", "values", "truncate", "distinct", "top", "with",
		    "from", "join", "inner join", "outer join", "left outer join", "right outer join", "left join", "right join", "cross join",
			"union", "except",
		    "where", "like", "between", "having", "exists",
		    "order by", "asc", "desc", "over", "group by", 
		    "on", "in", "is", "not", "as", "and", "or", "all",
			"create", "alter", "drop",
			"table", "function", "procedure", "view", "schema",
			"declare", "set",
			"if", "begin", "then", "else", "end", "for", "while", "null",
			"transaction", "commit", "rollback",
			"exec", "return", "returns", "print", "use",
			
			"bigint", "numeric", "bit", "smallint", "decimal", "smallmoney", "int", "tinyint", "money", "float", "real",
			"date", "datetimeoffset", "datetime2", "smalldatetime", "datetime", "time", "timestamp",
			"char", "varchar", "text", "nchar", "nvarchar", "ntext", 
			"binary", "varbinary", "image", 
			"cursor", "hierarchyid", "uniqueidentifier", "sql_variant", "xml"
		};

		private readonly List<string> functions = new List<string> {
		    "count", "count_big", "sum", "min", "max", "avg",
			"abs", "newid", "rand", "isnull", "coalesce",
			"left", "right", "substring", "ltrim", "rtrim", "upper", "lower", "charindex", "len", "stuff",
			"getdate", "dateadd", "datediff", "datepart", "datename",
			"convert", "cast",
			"row_number"
		};

		private readonly Regex variables = new Regex(@"(?:^|[""\s(+,=])(?<Variable>@[a-z0-9_]+)(?:$|[""\s)+,])", RegexOptions.IgnoreCase | RegexOptions.Multiline);


		private readonly IClassificationType keywordType;
		private readonly IClassificationType functionType;
		private readonly IClassificationType variableType;
		readonly ITagAggregator<NaturalTextTag> tagger;

		internal SqlClassifier(ITagAggregator<NaturalTextTag> tagger, IClassificationTypeRegistryService classificationRegistry)
		{
			this.tagger = tagger;
			keywordType = classificationRegistry.GetClassificationType("sql-keyword");
			functionType = classificationRegistry.GetClassificationType("sql-function");
			variableType = classificationRegistry.GetClassificationType("sql-variable");
		}

		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			IList<ClassificationSpan> classifiedSpans = new List<ClassificationSpan>();

			var tags = tagger.GetTags(span).ToList();
			foreach (IMappingTagSpan<NaturalTextTag> tagSpan in tags)
			{
				SnapshotSpan snapshot = tagSpan.Span.GetSpans(span.Snapshot).First();

				string text = snapshot.GetText().ToLowerInvariant();
				int index = -1;

				// keywords
				foreach (string keyword in keywords)
				{
					while (snapshot.Length > index + 1 && (index = text.IndexOf(keyword, index + 1)) > -1)
					{
						// controleren of het gevonden keyword niet tegen of in een ander woord staat
						if ((index > 0 && keywordPrefixCharacters.Contains(text[index - 1]) == false) ||
							(index + keyword.Length < text.Length && keywordPostfixCharacters.Contains(text[index + keyword.Length]) == false))
							continue;

						classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, keyword.Length), keywordType));
					}
				}

				// functions
				foreach (string function in functions)
				{
					while (snapshot.Length > index + 1 && (index = text.IndexOf(function, index + 1)) > -1)
					{
						// controleren of het gevonden keyword niet tegen of in een ander woord staat
						if ((index > 0 && functionPrefixCharacters.Contains(text[index - 1]) == false) ||
							(index + function.Length < text.Length && functionPostfixCharacters.Contains(text[index + function.Length]) == false))
							continue;

						classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, function.Length), functionType));
					}
				}

				// variables
				var matches = variables.Matches(text);
				foreach (Match match in matches)
					classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + match.Groups["Variable"].Index, match.Groups["Variable"].Length), variableType));
			}

			return classifiedSpans;
		}

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
		{
			add { }
			remove { }
		}
	}
}