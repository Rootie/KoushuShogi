// 
//  MainWindow2.cs
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
using System.Reflection;

namespace Shogiban
{
	public partial class MainWindow : Gtk.Window
	{
		private static string[] authors = new string[] { "Gerhard Götz <rootie232@googlemail.com>" };

		private Game game;
		private int CurMoveNr = -1;

		public MainWindow() : base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			
			game = new Game();
			UpdateMoveNavigationControls();
			
			game.GameStateChanged += HandleGameGameStateChanged;
			game.CurPlayerChanged += HandleGameCurPlayerChanged;
			game.MoveAdded += HandleGameMoveAdded;
			game.MoveRemoved += HandleGameMoveRemoved;
			game.MovesChanged += HandleGameMovesChanged;
			
			game.SetDefaultBoard();
			
			game.BlackPlayerEngine = new LocalPlayer();
			//game.BlackPlayerEngine = new GnuShogiPlayer();
			game.BlackPlayer.Name = "black";
			
			//game.WhitePlayerEngine = new LocalPlayer();
			game.WhitePlayerEngine = new GnuShogiPlayer();
			game.WhitePlayer.Name = "white";
			
			ShogibanBoardView.game = game;
			
			game.StartGame();
		}

		void HandleGameMovesChanged(object sender, EventArgs e)
		{
			(MovesCB.Model as ListStore).Clear();
			MovesCB.AppendText("Game Started");
			MovesCB.Active = 0;
			UpdateMoveNavigationControls();
		}

		void HandleGameMoveAdded(object sender, MoveAddedEventArgs e)
		{
			MovesCB.AppendText(game.Moves.Count + ": " + e.move.ToString());
			UpdateMoveNavigationControls();
		}

		void HandleGameMoveRemoved(object sender, EventArgs e)
		{
			MovesCB.RemoveText(game.Moves.Count + 1);
			UpdateMoveNavigationControls();
		}

		private String GetTurningPlayerString()
		{
			String str = String.Empty;
			str += game.CurPlayer == game.BlackPlayer ? "Black " : "White ";
			if (game.CurPlayer.Name != String.Empty)
			{
				str += "(" + game.CurPlayer.Name + ") ";
			}
			str += "moves.";
			return str;
		}

		private void UpdateMoveNavigationControls()
		{
			bool CanBack = CurMoveNr != 0 && game.Moves.Count > 0;
			bool CanForward = CurMoveNr >= 0 && CurMoveNr < game.Moves.Count;
			
			FirstMoveBtn.Sensitive = CanBack;
			PrevMoveBtn.Sensitive = CanBack;
			LastMoveBtn.Sensitive = CanForward;
			NextMoveBtn.Sensitive = CanForward;
			
			if (CurMoveNr < 0)
			{
				if (game.Moves.Count > 0)
					MovesCB.Active = game.Moves.Count;
			}
			else
			{
				MovesCB.Active = CurMoveNr;
			}
			
			ShogibanBoardView.ShowMoveNr(CurMoveNr);
		}

		private void SetStatusBarText(String Text)
		{
			statusbar.Pop(0);
			statusbar.Push(0, Text);
		}

		private void Quit()
		{
			game.BlackPlayerEngine.Dispose();
			game.WhitePlayerEngine.Dispose();
			
			Application.Quit();
		}

		private void HandleGameCurPlayerChanged(object sender, EventArgs e)
		{
			SetStatusBarText(GetTurningPlayerString());
		}

		private void HandleGameGameStateChanged(object sender, EventArgs e)
		{
			if (game.gameState == GameState.Review)
			{
				SetStatusBarText(game.GameFinishedReason);
			}
			else if (game.gameState == GameState.Playing)
			{
				SetStatusBarText("Game started. " + GetTurningPlayerString());
			}
		}

		protected virtual void OnDeleteEvent(object o, Gtk.DeleteEventArgs args)
		{
			Quit();
			args.RetVal = true;
		}

		protected virtual void OnQuitActionActivated(object sender, System.EventArgs e)
		{
			Quit();
		}

		protected virtual void OnAboutActionActivated (object sender, System.EventArgs e)
		{
			AboutDialog dialog = new AboutDialog ();

			Assembly asm = Assembly.GetExecutingAssembly ();
			
			dialog.ProgramName = (asm.GetCustomAttributes (
				typeof(AssemblyTitleAttribute), false)[0]
				as AssemblyTitleAttribute).Title;
			
			dialog.Version = asm.GetName ().Version.ToString ();
			
			dialog.Comments = (asm.GetCustomAttributes (
				typeof(AssemblyDescriptionAttribute), false)[0]
				as AssemblyDescriptionAttribute).Description;
			
			dialog.Copyright = (asm.GetCustomAttributes (
				typeof(AssemblyCopyrightAttribute), false)[0]
				as AssemblyCopyrightAttribute).Copyright;
			
			//TODO
			//dialog.License = license;
			
			dialog.Authors = authors;
			
			
			
			dialog.Run ();
			dialog.Destroy ();
		}
		
		protected virtual void OnNewGameActionActivated(object sender, System.EventArgs e)
		{
			game.SetDefaultBoard();
			game.StartGame();
		}
		
		protected virtual void OnSaveGameActionActivated(object sender, System.EventArgs e)
		{
			FileChooserDialog fd = new FileChooserDialog("Save shogi game", this, FileChooserAction.Save,
				                         				 Gtk.Stock.Cancel, ResponseType.Cancel,
		                                                 Gtk.Stock.Save, ResponseType.Accept);
			fd.DoOverwriteConfirmation = true;
			FileFilter filter;

			filter = new FileFilter();
			filter.Name = "All files";
			filter.AddPattern("*");
			fd.AddFilter(filter);

			filter = new FileFilter();
			filter.Name = "PSN files";
			filter.AddPattern("*.psn");
			fd.AddFilter(filter);
			fd.Filter = filter;
			
			if (fd.Run() == (int)ResponseType.Accept)
			{
				System.Console.WriteLine("saving file: " + fd.Filename);
				
				System.IO.FileStream stream = new System.IO.FileStream(fd.Filename, System.IO.FileMode.Create);
				Shogiban.FileFormat.PSN.Save(game, stream);
				stream.Close();
			}
			
			fd.Destroy();
		}
		
		protected virtual void OnUndoActionActivated(object sender, System.EventArgs e)
		{
			CurMoveNr = -1;
			game.Undo();
		}
		
		protected virtual void OnFirstMoveBtnClicked(object sender, System.EventArgs e)
		{
			CurMoveNr = 0;
			UpdateMoveNavigationControls();
		}
		
		protected virtual void OnPrevMoveBtnClicked(object sender, System.EventArgs e)
		{
			if (CurMoveNr > 0)
			{
				CurMoveNr = CurMoveNr - 1;
				UpdateMoveNavigationControls();
			}
			else if (CurMoveNr < 0)
			{
				CurMoveNr = game.Moves.Count - 1;
				UpdateMoveNavigationControls();
			}
		}
		
		protected virtual void OnNextMoveBtnClicked(object sender, System.EventArgs e)
		{
			if (CurMoveNr < game.Moves.Count && CurMoveNr >= 0)
			{
				CurMoveNr = CurMoveNr + 1;
				if (CurMoveNr == game.Moves.Count)
					CurMoveNr = -1;
				UpdateMoveNavigationControls();
			}
		}
		
		protected virtual void OnLastMoveBtnClicked(object sender, System.EventArgs e)
		{
			CurMoveNr = -1;
			UpdateMoveNavigationControls();
		}
		
		protected virtual void OnMovesCBChanged(object sender, System.EventArgs e)
		{
			if (MovesCB.Active == game.Moves.Count)
			{
				CurMoveNr = -1;
			}
			else
			{
				CurMoveNr = MovesCB.Active;
			}
			UpdateMoveNavigationControls();
		}
	}
}

