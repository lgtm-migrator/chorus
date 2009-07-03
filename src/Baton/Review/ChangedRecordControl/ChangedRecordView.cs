﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Chorus.merge;

namespace Baton.HistoryPanel.ChangedRecordControl
{
	public partial class ChangedRecordView : UserControl
	{
		public ChangedRecordView(Review.ChangedRecordSelectedEvent changedRecordSelectedEvent)
		{
			InitializeComponent();
			changedRecordSelectedEvent.Subscribe(r=>Load(r));
			_changeDescriptionRenderer.Navigated += webBrowser1_Navigated;
		}

		private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			//didn't work, 'cuase is't actually still being held by the browser
		  //  File.Delete(e.Url.AbsoluteUri.Replace(@"file:///", string.Empty));
		}

		public void Load(IChangeReport report)
		{
			if (report == null)
			{
			   // _changeDescriptionRenderer.Navigate(string.Empty);
			}
			else
			{
				var path = Path.GetTempFileName();
				File.WriteAllText(path, report.ToString()+" "+path);
				this._changeDescriptionRenderer.Navigate(path);
			}
		}
	}
}
