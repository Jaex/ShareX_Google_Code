﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (C) 2008-2013 ShareX Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using HelpersLib;
using ShareX.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UploadersLib;

namespace ShareX
{
    public partial class AfterUploadForm : Form
    {
        public TaskInfo Info { get; private set; }

        private UploadInfoParser parser = new UploadInfoParser();

        private ListViewGroup lvgForums = new ListViewGroup("Forums");
        private ListViewGroup lvgHtml = new ListViewGroup("HTML");
        private ListViewGroup lvgWiki = new ListViewGroup("Wiki");
        private ListViewGroup lvgLocal = new ListViewGroup("Local");
        private ListViewGroup lvgCustom = new ListViewGroup("Custom");

        public AfterUploadForm(TaskInfo info, bool autoClose = false)
        {
            InitializeComponent();
            Icon = Resources.ShareXIcon;
            Info = info;
            if (autoClose) tmrClose.Start();

            bool isFileExist = !string.IsNullOrEmpty(info.FilePath) && File.Exists(info.FilePath);

            if (info.DataType == EDataType.Image)
            {
                if (isFileExist)
                {
                    pbPreview.LoadImageFromFile(info.FilePath);
                }
                else
                {
                    pbPreview.LoadImageFromURL(info.Result.URL);
                }
            }

            Text = "ShareX - " + (isFileExist ? info.FilePath : info.FileName);

            lvClipboardFormats.Groups.Add(lvgForums);
            lvClipboardFormats.Groups.Add(lvgHtml);
            lvClipboardFormats.Groups.Add(lvgWiki);
            lvClipboardFormats.Groups.Add(lvgLocal);
            lvClipboardFormats.Groups.Add(lvgCustom);

            foreach (LinkFormatEnum type in Enum.GetValues(typeof(LinkFormatEnum)))
            {
                if (!Helpers.IsImageFile(Info.Result.URL) && type != LinkFormatEnum.URL && type != LinkFormatEnum.LocalFilePath && type != LinkFormatEnum.LocalFilePathUri)
                    continue;

                AddFormat(type.GetDescription(), GetUrlByType(type));
            }

            if (Helpers.IsImageFile(Info.Result.URL))
            {
                foreach (ClipboardFormat cf in Program.Settings.ClipboardContentFormats)
                {
                    AddFormat(cf.Description, parser.Parse(Info, cf.Format), lvgCustom);
                }
            }
        }

        private void AddFormat(string description, string text, ListViewGroup group = null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                ListViewItem lvi = new ListViewItem(description);

                if (group == null)
                {
                    if (description.Contains("HTML"))
                    {
                        lvi.Group = lvgHtml;
                    }
                    else if (description.Contains("Forums"))
                    {
                        lvi.Group = lvgForums;
                    }
                    else if (description.Contains("Local"))
                    {
                        lvi.Group = lvgLocal;
                    }
                    else if (description.Contains("Wiki"))
                    {
                        lvi.Group = lvgWiki;
                    }
                }
                else
                {
                    lvi.Group = group;
                }

                lvi.SubItems.Add(text);
                lvClipboardFormats.Items.Add(lvi);
                lvClipboardFormats.FillLastColumn();
            }
        }

        private void tmrClose_Tick(object sender, EventArgs e)
        {
            Close();
        }

        private void btnCopyImage_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Info.FilePath) && Helpers.IsImageFile(Info.FilePath) && File.Exists(Info.FilePath))
            {
                ClipboardHelper.CopyImageFromFile(Info.FilePath);
            }
        }

        private void btnCopyLink_Click(object sender, EventArgs e)
        {
            if (lvClipboardFormats.Items.Count > 0)
            {
                string url = null;

                if (lvClipboardFormats.SelectedItems.Count == 0)
                {
                    url = lvClipboardFormats.Items[0].SubItems[1].Text;
                }
                else if (lvClipboardFormats.SelectedItems.Count > 0)
                {
                    url = lvClipboardFormats.SelectedItems[0].SubItems[1].Text;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    ClipboardHelper.CopyText(url);
                }
            }
        }

        private void btnOpenLink_Click(object sender, EventArgs e)
        {
            string url = Info.Result.URL;

            if (!string.IsNullOrEmpty(url))
            {
                Helpers.LoadBrowserAsync(url);
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Info.FilePath) && File.Exists(Info.FilePath))
            {
                Helpers.LoadBrowserAsync(Info.FilePath);
            }
        }

        private void btnFolderOpen_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Info.FilePath) && File.Exists(Info.FilePath))
            {
                Helpers.OpenFolderWithFile(Info.FilePath);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region TaskInfo helper methods

        public string GetUrlByType(LinkFormatEnum type)
        {
            switch (type)
            {
                case LinkFormatEnum.URL:
                    return Info.Result.URL;
                case LinkFormatEnum.ShortenedURL:
                    return Info.Result.ShortenedURL;
                case LinkFormatEnum.ForumImage:
                    return parser.Parse(Info, UploadInfoParser.ForumImage);
                case LinkFormatEnum.HTMLImage:
                    return parser.Parse(Info, UploadInfoParser.HTMLImage);
                case LinkFormatEnum.WikiImage:
                    return parser.Parse(Info, UploadInfoParser.WikiImage);
                case LinkFormatEnum.ForumLinkedImage:
                    return parser.Parse(Info, UploadInfoParser.ForumLinkedImage);
                case LinkFormatEnum.HTMLLinkedImage:
                    return parser.Parse(Info, UploadInfoParser.HTMLLinkedImage);
                case LinkFormatEnum.WikiLinkedImage:
                    return parser.Parse(Info, UploadInfoParser.WikiLinkedImage);
                case LinkFormatEnum.ThumbnailURL:
                    return Info.Result.ThumbnailURL;
                case LinkFormatEnum.LocalFilePath:
                    return Info.FilePath;
                case LinkFormatEnum.LocalFilePathUri:
                    return GetLocalFilePathAsUri(Info.FilePath);
            }

            return Info.Result.URL;
        }

        public string GetLocalFilePathAsUri(string fp)
        {
            if (!string.IsNullOrEmpty(fp) && File.Exists(fp))
            {
                try
                {
                    return new Uri(fp).AbsoluteUri;
                }
                catch (Exception ex)
                {
                    DebugHelper.WriteException(ex);
                }
            }

            return string.Empty;
        }

        #endregion TaskInfo helper methods

        private void lvClipboardFormats_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && lvClipboardFormats.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvClipboardFormats.SelectedItems[0];
                string txt = lvi.SubItems[1].Text;
                if (!string.IsNullOrEmpty(txt))
                {
                    ClipboardHelper.CopyText(txt);
                }
            }
        }
    }
}