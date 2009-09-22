﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Chorus.Utilities;
using Chorus.Utilities.UsbDrive;
using Chorus.VcsDrivers.Mercurial;

namespace Chorus.clone
{
	/// <summary>
	/// Use this class to make an initial clone from a USB drive or Internet repository.
	/// Note, most clients can instead use the GetCloneDialog in Chorus.exe.
	/// </summary>
	public class CloneFromUsb
	{
		public CloneFromUsb()
		{
			DriveInfoRetriever = new RetrieveUsbDriveInfo();
		}

		/// <summary>
		/// Use this to insert an artificial drive info system for unit tests
		/// </summary>
		public IRetrieveUsbDriveInfo DriveInfoRetriever { get; set; }

		/// <summary>
		/// Use this to inject a custom filter, so that the only projects that can be chosen are ones
		/// you application is prepared to open.  The delegate is given the path to each mercurial project.
		/// </summary>
		public Func<string, bool> ProjectFilter = path => true;


		public bool GetHaveOneOrMoreUsbDrives()
		{
			return DriveInfoRetriever.GetDrives().Count > 0;
		}

		public IEnumerable<string> GetDirectoriesWithMecurialRepos()
		{
			foreach (var drive in DriveInfoRetriever.GetDrives())
			{
#if MONO
				if(drive.RootDirectory.FullName.Trim() == "/.")
				{
					continue; // bug in our usb-finding code is returning the root directory
				}
#endif
				string[] directories = new string[0];
				try
				{ // this is all complicated because the yield can't be inside the try/catch
					directories = Directory.GetDirectories(drive.RootDirectory.FullName);
				}
				catch (Exception error)
				{
					MessageBox.Show(
						string.Format("Error while looking at USB flash drive.  The drive root was {0}. The error was: {1}",
									  drive.RootDirectory.FullName, error.Message), "Error", MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				}
				foreach (var dir in directories)
				{
					if (Directory.Exists(Path.Combine(dir, ".hg")) && ProjectFilter(dir))
					{
						yield return dir;
					}
					else //we'll look just at the next level down
					{
						string[] subdirs = new string[0];
						try
						{    // this is all complicated because the yield can't be inside the try/catch
							subdirs = Directory.GetDirectories(dir);
						}
						catch (Exception error)
						{
							MessageBox.Show(
								string.Format(
									"Error while looking at usb drive.  The drive root was {0}, the directory was {1}. The error was: {2}",
									drive.RootDirectory.FullName, dir, error.Message), "Error", MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						}
						foreach (var subdir in subdirs)
						{
							if (Directory.Exists(Path.Combine(subdir, ".hg")) && ProjectFilter(subdir))
							{
								yield return subdir;
							}
						}

					}
				}
			}

		}

		public string MakeClone(string sourcePath, string parentDirectoryToPutCloneIn, IProgress progress)
		{
			var target = Path.Combine(parentDirectoryToPutCloneIn, Path.GetFileName(sourcePath));
			if(Directory.Exists(target))
				throw new ApplicationException("Cannot clone onto an existing directory ("+target+")");

			var repo = new HgRepository(sourcePath, progress);

			repo.CloneLocal(target);
			return target;
		}

	}
}