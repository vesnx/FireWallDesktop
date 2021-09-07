// ***********************************************************************
// Assembly         : Desktop
// Author           : Walter Verhoeven
// Created          : 07-27-2021
//
// Last Modified By : Walter Verhoeven
// Last Modified On : 09-04-2021
// ***********************************************************************
// <copyright file="PdfViewerViewModel.cs" company="VESNX AG">
//     © 2021 Walter Verhoeven, all rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Desktop.Model;
using Desktop.Services.Interfaces;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.IO;

namespace Desktop.Dialogs
{
    /// <summary>
    /// Class PdfViewerViewModel.
    /// Implements the <see cref="Desktop.Dialogs.DialogBase" />
    /// </summary>
    /// <seealso cref="Desktop.Dialogs.DialogBase" />
    public class PdfViewerViewModel : DialogBase
    {
        /// <summary>
        /// The PDF
        /// </summary>
        internal const string PDF = "PDF";
        /// <summary>
        /// The file name
        /// </summary>
        private string _fileName;
        /// <summary>
        /// The stream
        /// </summary>
        private Stream _stream;
        /// <summary>
        /// The show files toolbar
        /// </summary>
        private bool _showFilesToolbar;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfViewerViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="service">The service.</param>
        /// <param name="eventAggregator">The event aggregator.</param>
        public PdfViewerViewModel(IDialogService dialogService, IDataService service, IEventAggregator eventAggregator)
: base(dialogService, service, eventAggregator)
        {
            ShowFilesToolbar = RunTime.SelectedFireWall.User.Role.HasFlag(AdminAccess.Guest) == false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show files toolbar].
        /// </summary>
        /// <value><c>true</c> if [show files toolbar]; otherwise, <c>false</c>.</value>
        public bool ShowFilesToolbar { get => _showFilesToolbar; set => SetProperty(ref _showFilesToolbar , value); }
        /// <summary>
        /// Gets the set tool bars.
        /// </summary>
        /// <value>The set tool bars.</value>
        public DelegateCommand<object[]> SetToolBars { get; private set; }
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get => _fileName; set => SetProperty(ref _fileName, value); }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        /// <value>The document.</value>
        public Stream Document { get => _stream; set => SetProperty(ref _stream, value); }

        /// <summary>
        /// overwrite the actions to populate the dialog
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            //released by owner calling the dialog
            FileName = parameters.GetValue<FileInfo>(PDF).FullName;
        }
    }
}
