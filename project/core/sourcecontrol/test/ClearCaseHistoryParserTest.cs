using System;
using NUnit.Framework;

namespace ThoughtWorks.CruiseControl.Core.Sourcecontrol.Test
{
	[TestFixture]
	public class ClearCaseHistoryParserTest 
	{

		ClearCaseHistoryParser _parser;

		[SetUp]
		protected void Setup()
		{
			_parser = new ClearCaseHistoryParser();
		}

		[Test]
		public void CanTokenizeWithNoComment()
		{
			string[] tokens = _parser.TokenizeEntry( @"ppunjani#~#Friday, September 27, 2002 06:31:36 PM#~#D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\context.js#~#\main\0#~#mkelem#~#!#~#!#~#" );
			Assert.AreEqual( 8, tokens.Length );
			Assert.AreEqual( "ppunjani", tokens[ 0 ] );
			Assert.AreEqual( "Friday, September 27, 2002 06:31:36 PM", tokens[ 1 ] );
			Assert.AreEqual( @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\context.js", tokens[ 2 ] );
			Assert.AreEqual( @"\main\0", tokens[ 3 ] );
			Assert.AreEqual("mkelem", tokens[ 4 ] );
			Assert.AreEqual( "!", tokens[ 5 ] );
			Assert.AreEqual( "!", tokens[ 6 ] );
			Assert.AreEqual( string.Empty, tokens[ 7 ] );
		}

		[Test]
		public void CanCreateNewModification()
		{
			const string userName = "gsmith";
			const string timeStamp = "Wednesday, March 10, 2004 08:52:05 AM";
			DateTime expectedTime = new DateTime( 2004, 03, 10, 08, 52, 05 );
			const string path = @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common";
			const string file = "context.js";
			const string elementName = path + "\\" + file;
			const string modificationType = "checkin";
			const string comment = "implemented dwim";
			Modification modification = _parser.CreateNewModification( userName,
				timeStamp,
				elementName,
				modificationType,
				comment );

			Assert.AreEqual( comment, modification.Comment );
			Assert.IsNull( modification.EmailAddress );
			Assert.AreEqual( file, modification.FileName );
			Assert.AreEqual( path, modification.FolderName );
			Assert.AreEqual( expectedTime, modification.ModifiedTime );
			Assert.AreEqual( modificationType, modification.Type );
			Assert.IsNull( modification.Url );
			Assert.AreEqual( userName, modification.UserName );
		}

		[Test]
		public void CanAssignFileInfo()
		{
			const string path = @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common";
			const string file = "context.js";
			const string fullPath = path + "\\" + file;
			Modification modification = new Modification();

			_parser.AssignFileInfo( modification, fullPath );

			Assert.AreEqual( path, modification.FolderName );
			Assert.AreEqual( file, modification.FileName );
		}

		
		[Test]
		public void CanAssignFileInfoWithNoPath()
		{
			const string file = "context.js";
			Modification modification = new Modification();

			_parser.AssignFileInfo( modification, file );

			Assert.AreEqual( string.Empty, modification.FolderName );
			Assert.AreEqual( file, modification.FileName );
		}

		[Test]
		public void CanAssignModificationTime()
		{
			const string time = "Friday, September 27, 2002 06:31:38 PM";
			Modification modification = new Modification();

			_parser.AssignModificationTime( modification, time );

			Assert.AreEqual( new DateTime( 2002, 09, 27, 18, 31, 38 ), modification.ModifiedTime );
		}

		[Test]
		public void CanAssignModificationTimeWithBadTime()
		{
			const string time = "not a valid time";
			Modification modification = new Modification();

			_parser.AssignModificationTime( modification, time );

			Assert.AreEqual( new DateTime(), modification.ModifiedTime );
		}
		
		[Test]
		public void CanIgnoreBranchEvent()
		{
			Modification modification = _parser.ParseEntry( "Hi, I'm an invalid entry" );
			Assert.IsNull( modification );
		}

		[Test]
		public void CanParseBadEntry()
		{
			Modification modification = _parser.ParseEntry( @"ppunjani#~#Tuesday, February 18, 2003 05:09:14 PM#~#D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\wwhpagef.js#~#\main\0#~#mkbranch#~#!#~#!#~#" );
			Assert.IsNull( modification );
		}

		[Test]
		public void CanParse()
		{
			Modification[] mods = _parser.Parse( ClearCaseMother.ContentReader, ClearCaseMother.OLDEST_ENTRY, ClearCaseMother.NEWEST_ENTRY);
			Assert.IsNotNull( mods, "mods should not be null" );
			Assert.AreEqual( 28, mods.Length );			
		}

		[Test]
		public void CanParseEntry()
		{
			Modification modification = _parser.ParseEntry( @"ppunjani#~#Wednesday, November 20, 2002 07:37:22 PM#~#D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\towwhdir.js#~#\main#~#mkelem#~#!#~#!#~#" );
			Assert.AreEqual( "ppunjani", modification.UserName );
			Assert.AreEqual( new DateTime( 2002, 11, 20, 19, 37, 22), modification.ModifiedTime );
			Assert.AreEqual( @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common", modification.FolderName );
			Assert.AreEqual( "towwhdir.js", modification.FileName );
			Assert.AreEqual( "mkelem", modification.Type );
			Assert.IsNull( modification.Comment );
		}
		
		[Test]
		public void CanParseEntryWithNoComment()
		{
			Modification modification = _parser.ParseEntry( @"ppunjani#~#Wednesday, February 25, 2004 01:09:36 PM#~#D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\topics.js#~##~#**null operation kind**#~#!#~#!#~#" );
			Assert.AreEqual( "ppunjani", modification.UserName);
			Assert.AreEqual( new DateTime( 2004, 02, 25, 13, 09, 36 ), modification.ModifiedTime);
			Assert.AreEqual( @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common", modification.FolderName);
			Assert.AreEqual( "topics.js", modification.FileName);
			Assert.AreEqual( "**null operation kind**", modification.Type );
			Assert.IsNull( modification.Comment );
		}
		
		[Test]
		public void CanTokenize()
		{
			string[] tokens = _parser.TokenizeEntry( @"ppunjani#~#Friday, March 21, 2003 03:32:24 PM#~#D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\files.js#~##~#mkelem#~#!#~#!#~#made from flat file" );
			Assert.AreEqual( 8, tokens.Length );
			Assert.AreEqual( "ppunjani", tokens[0] );
			Assert.AreEqual( "Friday, March 21, 2003 03:32:24 PM", tokens[1] );
			Assert.AreEqual( @"D:\CCase\ppunjani_view\RefArch\tutorial\wwhdata\common\files.js", tokens[2] );
			Assert.AreEqual( string.Empty, tokens[3] );
			Assert.AreEqual( "mkelem", tokens[4] );
			Assert.AreEqual( "!", tokens[5] );
			Assert.AreEqual( "!", tokens[6] );
			Assert.AreEqual( "made from flat file", tokens[7] );
		}
	}
}
