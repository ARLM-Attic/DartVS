﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace DanTup.DartVS
{
	[InstalledProductRegistration("DanTup's DartVS: Visual Studio support for Google's Dart", @"Some support for coding Dart in Visual Studio.", "0.5")]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
	[ProvideLanguageService(typeof(DartLanguageInfo), "Dart", 100)]
	[ProvideLanguageExtension(typeof(DartLanguageInfo), ".dart")]
	public sealed class DartPackage : Package
	{
		[Import]
		DartVsAnalysisService analysisService = null;

		[Import]
		ITextDocumentFactoryService textDocumentFactory = null;

		[Import]
		IVsEditorAdaptersFactoryService editorAdapterFactory = null;

		DartErrorListProvider errorProvider;

		// TODO: Handle file renames properly (errors stick around)
		// TODO: Handle closing projects/solutions (errors stick around)

		protected override void Initialize()
		{
			base.Initialize();

			// Force initialisation of [Imports] on this class.
			var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
			componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

			// Wire up the Error Provider to the notifications from the service.
			errorProvider = new DartErrorListProvider(this);
			analysisService.AnalysisErrorsNotification.Subscribe(errorProvider.UpdateErrors);

			// Register icons so they show in the solution explorer nicely.
			IconRegistration.RegisterIcons();

			((IServiceContainer)this).AddService(typeof(DartLanguageInfo), new DartLanguageInfo(textDocumentFactory, editorAdapterFactory, analysisService), true);
		}

		protected override void Dispose(bool disposing)
		{
			analysisService.Dispose();
		}

		public static T GetGlobalService<T>(Type type = null) where T : class
		{
			return Package.GetGlobalService(type ?? typeof(T)) as T;
		}
	}
}
