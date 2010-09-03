// 
//  Main.cs
//  
//  Author:
//       Gerhard Götz <rootie232@googlemail.com>
//  
//  Copyright (c) 2010 Gerhard Götz
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using Gtk;

namespace Shogiban
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//GLib.ExceptionManager.UnhandledException += HandleGLibExceptionManagerUnhandledException;
			Application.Init();
			using (MainWindow win = new MainWindow())
			{
				win.Show();
				Application.Run();
			}
		}

		/*
		static void HandleGLibExceptionManagerUnhandledException (GLib.UnhandledExceptionArgs args)
		{
		}
		*/
	}
}
