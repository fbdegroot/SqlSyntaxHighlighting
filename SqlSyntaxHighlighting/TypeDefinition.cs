using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace SqlSyntaxHighlighting
{
	public static class TypeDefinition
	{
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("sql-keyword")]
		internal static ClassificationTypeDefinition SqlKeywordType;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "sql-keyword")]
		[Name("SQL Syntax Highlighting - Keyword")]
		[DisplayName("SQL Syntax Highlighting - Keyword")]
		[UserVisible(true)]
		[Order(Before = Priority.High, After = Priority.High)]
		internal sealed class SqlKeywordFormat : ClassificationFormatDefinition
		{
			public SqlKeywordFormat()
			{
				this.ForegroundColor = Colors.Blue;
			}
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("sql-function")]
		internal static ClassificationTypeDefinition SqlFunctionType;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "sql-function")]
		[Name("SQL Syntax Highlighting - Function")]
		[DisplayName("SQL Syntax Highlighting - Function")]
		[UserVisible(true)]
		[Order(Before = Priority.High, After = Priority.High)]
		internal sealed class SqlFunctionFormat : ClassificationFormatDefinition
		{
			public SqlFunctionFormat()
			{
				this.ForegroundColor = Colors.Magenta;
			}
		}

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("sql-variable")]
		internal static ClassificationTypeDefinition SqlVariableType;

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = "sql-variable")]
		[Name("SQL Syntax Highlighting - Variable")]
		[DisplayName("SQL Syntax Highlighting - Variable")]
		[UserVisible(true)]
		[Order(Before = Priority.High, After = Priority.High)]
		internal sealed class SqlVariableFormat : ClassificationFormatDefinition
		{
			public SqlVariableFormat()
			{
				this.ForegroundColor = Colors.Green;
			}
		}
	}
}