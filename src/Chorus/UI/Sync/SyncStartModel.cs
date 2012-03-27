﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Chorus.Properties;
using Chorus.VcsDrivers;
using Chorus.VcsDrivers.Mercurial;
using Palaso.IO;
using Palaso.Progress.LogBox;

namespace Chorus.UI.Sync
{
	internal class SyncStartModel
	{
		private readonly HgRepository _repository;

		public SyncStartModel(HgRepository repository)
		{
			_repository = repository;
		}

		public bool GetInternetStatusLink(out string buttonLabel, out string message, out string tooltip)
		{
			buttonLabel = Resources.ksInternetButtonLabel;
			RepositoryAddress address;
			try
			{
				address = _repository.GetDefaultNetworkAddress<HttpRepositoryPath>();
			}
			catch (Exception error)//probably, hgrc is locked
			{
				message = error.Message;
				tooltip = string.Empty;
				return false;
			}

			bool ready = _repository.GetIsReadyForInternetSendReceive(out tooltip);
			if (ready)
			{
				buttonLabel = address.Name;
				message = string.Empty;

				// But, the Internet might be down or the repo unreachable.
				if (!IsInternetRepositoryReachable(address))
				{
					message = Resources.ksNoInternetAccess;
					tooltip = string.Empty;
					return false;
				}
			}
			else
			{
				message = tooltip;
			}

			return ready;
		}

		private bool IsInternetRepositoryReachable(RepositoryAddress repoAddress)
		{
			return repoAddress.CanConnect(_repository, repoAddress.Name, new NullProgress());
		}

		public bool GetNetworkStatusLink(out string message, out string tooltip)
		{
			RepositoryAddress address;
			var ready = false;
			message = string.Empty;

			try
			{
				address = _repository.GetDefaultNetworkAddress<DirectoryRepositorySource>();
			}
			catch (Exception error)//probably, hgrc is locked
			{
				message = error.Message;
				tooltip = string.Empty;
				return false;
			}
			if (address == null)
				message = Resources.ksSharedFolderNotAssociated;
			else
				ready = IsSharedFolderRepositoryReachable(address);
			if (ready)
			{
				message = string.Empty;
				tooltip = address.URI;
			}
			else
			{
				if (address != null)
					message = Resources.ksSharedFolderInaccessible;
				tooltip = message;
			}

			return ready;
		}

		private bool IsSharedFolderRepositoryReachable(RepositoryAddress repoAddress)
		{
			// We want to know if we can connect, but we don't want to bother the user with extraneous information.
			return repoAddress.CanConnect(_repository, repoAddress.Name, new NullProgress());
		}

		internal bool GetUsbStatusLink(IUsbDriveLocator usbDriveLocator, out string message)
		{
			var ready = false;
			if (!usbDriveLocator.UsbDrives.Any())
			{
				message = "First insert a USB flash drive.";
			}
			else if (usbDriveLocator.UsbDrives.Count() > 1)
			{
				message = "More than one USB drive detected. Please remove one.";
			}
			else
			{
				try
				{
					var first = usbDriveLocator.UsbDrives.First();
#if !MONO
					message = first.RootDirectory + " " + first.VolumeLabel + " (" +
										   Math.Floor(first.TotalFreeSpace / 1024000.0) + " Megs Free Space)";
#else
					message = first.VolumeLabel;
					//RootDir & volume label are the same on linux.  TotalFreeSpace is, like, maxint or something in mono 2.0
#endif
					ready = true;
				}
				catch (Exception error)
				{
					message = error.Message;
					ready = false;
				}
			}
			return ready;
		}

		public void SetNewSharedNetworkAddress(HgRepository repository, string path)
		{
			if (string.IsNullOrEmpty(path))
				return;
			try
			{
				if (!Directory.Exists(Path.Combine(path, ".hg")))
				{
					if (DirectoryUtilities.GetSafeDirectories(path).Length > 0 || Directory.GetFiles(path).Length > 0)
					{
						Palaso.Reporting.ErrorReport.NotifyUserOfProblem(
							"The folder you chose doesn't have a repository. Chorus cannot make one there, because the folder is not empty.  Please choose a folder that is already being used for send/receive, or create and choose a new folder to hold the repository.");
						return;
					}

					var result = MessageBox.Show("A new repository will be created in " + path + ".", "Create new repository?",
									MessageBoxButtons.OKCancel);
					if (result != DialogResult.OK)
						return;

				}
				string alias = HgRepository.GetAliasFromPath(path);
				repository.SetTheOnlyAddressOfThisType(RepositoryAddress.Create(alias, path));
			}
			catch (Exception e)
			{
				Palaso.Reporting.ErrorReport.NotifyUserOfProblem(e, "There was a problem setting the network path.");
				throw;
			}
		}
	}
}
