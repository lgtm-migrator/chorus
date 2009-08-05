using System;
using System.IO;
using System.Text;
using System.Xml;
using Chorus.merge.xml.generic;
using Chorus.VcsDrivers;

namespace Chorus.merge
{

	public interface IConflict
	{
		//store a descriptor that can be used later to find the element again, as when reviewing conflict.
		//for xml files, this would be an xpath which returns the element which you'd use to
		//show the difference to the user
   //     string PathToUnitOfConflict { get; set; }
		string RelativeFilePath { get; }

		ContextDescriptor Context { get; set; }
		string GetFullHumanReadableDescription();
		string Description
		{
			get;
		}

		string WinnerId
		{
			get;
		}
		Guid  Guid { get; }
		MergeSituation Situation { get; set; }
		string RevisionWhereMergeWasCheckedIn { get;  }

		string GetConflictingRecordOutOfSourceControl(IRetrieveFileVersionsFromRepository fileRetriever, ThreeWayMergeSources.Source mergeSource);
		void WriteAsXml(XmlWriter writer);
	}

	public class TypeGuidAttribute : Attribute
	{
		public TypeGuidAttribute(string guid)
		{
			GuidString = guid;
		}
		public string GuidString { get; private set; }
	}

	[TypeGuid("18C7E1A2-2F69-442F-9057-6B3AC9833675")]
	public class UnmergableFileTypeConflict :Conflict
	{
		public UnmergableFileTypeConflict(MergeSituation situation )
			: base(situation)
		{
		}


		public override string GetFullHumanReadableDescription()
		{
			var b = new StringBuilder();
			b.AppendFormat("Chorus did not have the ability to merge both user's version of the file {0}", Situation.PathToFileInRepository);
			b.AppendLine();

			string loserId = (Situation.ConflictHandlingMode != MergeOrder.ConflictHandlingModeChoices.TheyWin)
								  ?
									 Situation.UserYId
								  :Situation.UserXId;

			b.AppendFormat("The merger gave both users the copy from '{0}'.", WinnerId);
			b.AppendLine();
			b.AppendFormat("The version from '{0}' is not lost; it is available in the Chorus repository", loserId);
			return b.ToString();
		}



		public override string Description
		{
			get { return "Merge Failure"; }
		}

		public override string GetConflictingRecordOutOfSourceControl(IRetrieveFileVersionsFromRepository fileRetriever, ThreeWayMergeSources.Source mergeSource)
		{
			throw new NotImplementedException();
		}
	}

	public class ThreeWayMergeSources
	{
		public enum Source
		{
			Ancestor, UserX, UserY
		}
	}

}