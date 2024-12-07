using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Plugin.TelegramBot
{
	internal static class Utils
	{
		public static String[] Split(String source, Int32 length)
		{
			String message;
			if(source != null && source.Length > length)
			{
				List<String> result = new List<String>();
				Int32 startIndex = 0;
				while(startIndex < source.Length)
				{
					Int32 substringLen = (startIndex + length) < source.Length
					? length
					: source.Length - startIndex;

					message = source.Substring(startIndex, substringLen);
					startIndex += substringLen;

					result.Add(message.Trim('\r','\n','\t'));
				}

				return result.ToArray();
			} else
				return new String[] { source };
		}

		/// <summary>Скомпиленная регулярка для удаления тегов из текста</summary>
		private readonly static Regex RemoveTagsRegex = new Regex(@"</?([^>]+)>", RegexOptions.Compiled);

		public static String FormatHtml(String html)
			=> Utils.FormatHtml(html, new String[] { "b", "i", "u", "s", "a", "code", "pre", });

		/// <summary>Отформатировать все теги из HTML сообщения</summary>
		/// <param name="html">Текст в HTML формате из которого удалить лишние теги</param>
		/// <param name="safeTags">Массив тегов, которые не нужно убирать</param>
		/// <returns>Результат без HTML содержимого</returns>
		public static String FormatHtml(String html, String[] safeTags)
		{
			if(String.IsNullOrEmpty(html))
				return html;
			else
			{
				MatchCollection matches = RemoveTagsRegex.Matches(html);

				for(Int32 loop = matches.Count - 1; loop >= 0; loop--)
				{
					Match match = matches[loop];
					if(safeTags != null && Array.Exists(safeTags, delegate (String p) { return match.Groups[1].Value.Equals(p, StringComparison.OrdinalIgnoreCase) || match.Groups[1].Value.StartsWith(p + " ", StringComparison.InvariantCultureIgnoreCase); }))
						continue;

					html = html.Remove(match.Index, match.Length);
					html = html.Insert(match.Index, match.Value.Replace("<", "&lt;").Replace(">", "&gt;"));
				}
				return html;
			}
		}

		public static Boolean TryChangeValue(String text, ParameterInfo parameter, out Object value)
		{
			value = null;
			Boolean result = false;
			TypeConverter converter = TypeDescriptor.GetConverter(parameter.ParameterType);
			try
			{
				value = converter.ConvertFromString(text);

				//value = Convert.ChangeType(text, parameter.ParameterType);
				result = true;
			} catch(IndexOutOfRangeException)
			{
			} catch(NotSupportedException)
			{
			} catch(FormatException)
			{
			} catch(Exception exc)
			{
				Exception innerException = exc.InnerException;
				if(innerException != null
					&& !(innerException is IndexOutOfRangeException)
					&& !(innerException is FormatException))
					throw;
			}

			if(!result && String.IsNullOrEmpty(text) && parameter.HasDefaultValue)
			{
				value = parameter.DefaultValue;
				result = true;
			}
			return result;
		}
	}
}