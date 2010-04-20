﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Chorus.FileTypeHanders.xml;
using Chorus.merge.xml.generic;
using Chorus.VcsDrivers.Mercurial;

namespace Chorus.FileTypeHanders.FieldWorks
{
	/// <summary>
	/// Class that handles diffing of two versions for FieldWorks 7.0 xml data.
	/// </summary>
	public class FieldWorks2WayDiffer
	{
		private readonly IMergeEventListener m_eventListener;
		private readonly FileInRevision m_parentFileInRevision;
		private readonly FileInRevision m_childFileInRevision;
		private string m_parentXml;
		private string m_childXml;

		public static FieldWorks2WayDiffer CreateFromFileInRevision(FileInRevision parent, FileInRevision child, IMergeEventListener changeAndConflictAccumulator, HgRepository repository)
		{
			return new FieldWorks2WayDiffer(parent.GetFileContents(repository), child.GetFileContents(repository), changeAndConflictAccumulator, parent, child);
		}
		/// <summary>Used by unit tests only.</summary>
		public static FieldWorks2WayDiffer CreateFromStrings(string parentXml, string childXml, IMergeEventListener eventListener)
		{
			return new FieldWorks2WayDiffer(parentXml, childXml, eventListener);
		}

		private FieldWorks2WayDiffer(string parentXml, string childXml,IMergeEventListener eventListener)
		{
			m_parentFileInRevision = null;
			m_childFileInRevision = null;
			m_parentXml = parentXml;
			m_childXml = childXml;
			m_eventListener = eventListener;
		}

		private FieldWorks2WayDiffer(string parentXml, string childXml, IMergeEventListener eventListener, FileInRevision parent, FileInRevision child)
			: this(parentXml, childXml, eventListener)
		{
			m_parentFileInRevision = parent;
			m_childFileInRevision = child;
		}

		public void ReportDifferencesToListener()
		{
			var parentIndex = new Dictionary<string, string>();
			PrepareIndex(parentIndex, m_parentXml);
			m_parentXml = null;
			var childIndex = new Dictionary<string, string>();
			PrepareIndex(childIndex, m_childXml);
			m_childXml = null;

			var parentDoc = new XmlDocument();
			var childDoc = new XmlDocument();
#if !ORIGINAL
			var keys = new List<string>();
			// Check for new <rt> elements in child.
			foreach (var kvpChild in childIndex.Where(kvp => !parentIndex.ContainsKey(kvp.Key)))
			{
				m_eventListener.ChangeOccurred(new XmlAdditionChangeReport(
												m_childFileInRevision,
												XmlUtilities.GetDocumentNodeFromRawXml(kvpChild.Value, childDoc),
												null)); // url for final parm, maybe.
				keys.Add(kvpChild.Key);
			}
			// Remove new items from child index.
			foreach (var key in keys)
				childIndex.Remove(key);
			keys.Clear();

			// Check for deleted <rt> elements in child.
			foreach (var kvpParent in parentIndex.Where(kvp => !childIndex.ContainsKey(kvp.Key)))
			{
				m_eventListener.ChangeOccurred(new XmlDeletionChangeReport(
												m_parentFileInRevision,
												XmlUtilities.GetDocumentNodeFromRawXml(kvpParent.Value, parentDoc),
												null)); // url for final parm, maybe.
				keys.Add(kvpParent.Key);
			}
			// Remove deleted items from parent index.
			foreach (var key in keys)
				parentIndex.Remove(key);
			keys.Clear();

			// Check for changed <rt> elements in child.
			foreach (var kvpParent in parentIndex)
			{
				var parentKey = kvpParent.Key;
				var parentValue = kvpParent.Value;
				var childValue = childIndex[parentKey];
				if (parentValue != childValue)
				{
					var parentNode = XmlUtilities.GetDocumentNodeFromRawXml(parentValue, parentDoc);
					var childNode = XmlUtilities.GetDocumentNodeFromRawXml(childValue, childDoc);
					if (!XmlUtilities.AreXmlElementsEqual(childNode, parentNode))
					{
						// Child has changed.
						m_eventListener.ChangeOccurred(new XmlChangedRecordReport(
														m_parentFileInRevision,
														m_childFileInRevision,
														parentNode,
														childNode,
														null)); // url for final parm, maybe.
					}
				}
				childIndex.Remove(parentKey);
			}
			parentIndex.Clear();
#else
			foreach (var kvpParent in parentIndex)
			{
				var parentKey = kvpParent.Key;
				var parentValue = kvpParent.Value;
				string childValue;
				if (childIndex.TryGetValue(parentKey, out childValue))
				{
					if (parentValue != childValue)
					{
						var parentNode = XmlUtilities.GetDocumentNodeFromRawXml(parentValue, parentDoc);
						var childNode = XmlUtilities.GetDocumentNodeFromRawXml(childValue, childDoc);
						if (!XmlUtilities.AreXmlElementsEqual(childNode, parentNode))
						{
							// Child has changed.
							m_eventListener.ChangeOccurred(new XmlChangedRecordReport(
															m_parentFileInRevision,
															m_childFileInRevision,
															parentNode,
															childNode,
															null)); // url for final parm, maybe.
						}
					}
					// 'else' means the xml is the same, so no difference.
					childIndex.Remove(parentKey);
				}
				else
				{
					m_eventListener.ChangeOccurred(new XmlDeletionChangeReport(
													m_parentFileInRevision,
													XmlUtilities.GetDocumentNodeFromRawXml(kvpParent.Value, parentDoc),
													null)); // url for final parm, maybe.
				}
			}
			foreach (var child in childIndex.Values)
			{
				m_eventListener.ChangeOccurred(new XmlAdditionChangeReport(
												m_childFileInRevision,
												XmlUtilities.GetDocumentNodeFromRawXml(child, childDoc),
												null)); // url for final parm, maybe.
			}
#endif
		}

		private static void PrepareIndex(IDictionary dictionary, string fwData)
		{
#if USEXMLREADER
			// Try using an XmlReader.
			var settings = new XmlReaderSettings { ValidationType = ValidationType.None };
			using (var reader = XmlReader.Create(new StringReader(fwData), settings))
			{
				reader.MoveToContent();
				while (reader.Read())
				{
					if (reader.LocalName != "rt")
						continue;
					var key = reader.GetAttribute("guid");
					var value = reader.ReadOuterXml();
					dictionary.Add(key, value);
				}
			}
#else
			// Try working through the string, directly.
			const string guidAttr = "guid=";
			const string openRt = "<rt";
			var startOfRtElementOffset = fwData.IndexOf(openRt);
			const string closeRt = "</rt>";
			while (startOfRtElementOffset > 0)
			{
				var endOfRtElementOffset = fwData.IndexOf(closeRt, startOfRtElementOffset + 3);
				var lengthToCopy = endOfRtElementOffset - startOfRtElementOffset + 5;
				var rtElement = fwData.Substring(startOfRtElementOffset, lengthToCopy);
				var guidStartOffset = rtElement.IndexOf(guidAttr) + 6;
				var guidAsString = rtElement.Substring(guidStartOffset, 36);
				dictionary.Add(guidAsString, rtElement);
				startOfRtElementOffset = fwData.IndexOf(openRt, endOfRtElementOffset);
			}
#endif
		}
	}
}
