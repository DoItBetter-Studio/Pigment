using System;
using System.Windows.Forms;

namespace Glyphborn.Pigment
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// To customize application configuration such as set high DPI settings or default font,
			// see https://aka.ms/applicationconfiguration.
			try
			{
				ApplicationConfiguration.Initialize();

				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
				Application.ThreadException += (s, e) =>
					MessageBox.Show(e.Exception.ToString(), "Thread Exception");
				AppDomain.CurrentDomain.UnhandledException += (s, e) =>
					MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled Exception");

				Application.Run(new Pigment());
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), "Fatal Error");
			}
		}
	}
}