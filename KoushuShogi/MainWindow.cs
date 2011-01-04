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
using System.Text;

namespace Shogiban
{
	public partial class MainWindow : Gtk.Window
	{
		private static string[] authors = new string[] { "Gerhard Götz <rootie232@googlemail.com>" };

		private Game game;
		private uint MoveCount = 0;
		private int CurMoveNr = -1;

		public MainWindow() : base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			
			BlackTimeLblBox.ModifyBg(StateType.Normal, new Gdk.Color(0, 0, 0));
			BlackTimeLbl.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
			WhiteTimeLblBox.ModifyBg(StateType.Normal, new Gdk.Color(255, 255, 255));
			WhiteTimeLbl.ModifyFg(StateType.Normal, new Gdk.Color(0, 0, 0));
			
			game = new Game();
			UpdateMoveNavigationControls();
			
			game.GameStateChanged += HandleGameGameStateChanged;
			game.PositionChanged += HandleGamePositionChanged;
			game.MoveAdded += HandleGameMoveAdded;
			game.MoveRemoved += HandleGameMoveRemoved;
			game.MovesChanged += HandleGameMovesChanged;
			
			ShogibanBoardView.game = game;
		}

		void HandleGameMovesChanged(object sender, EventArgs e)
		{
			MoveCount = 0;
			(MovesCB.Model as ListStore).Clear();
			MovesCB.AppendText("Game Started");
			while (MoveCount < game.Moves.Count)
			{
				MovesCB.AppendText(MoveCount + ": " + game.Moves[(int)MoveCount].move.ToString());
				MoveCount++;
			}
			MovesCB.Active = (int)MoveCount;
			UpdateMoveNavigationControls();
		}

		void HandleGameMoveAdded(object sender, MoveAddedEventArgs e)
		{
			MoveCount++;
			MovesCB.AppendText(game.Moves.Count + ": " + e.move.ToString());
			UpdateMoveNavigationControls();
		}

		void HandleGameMoveRemoved(object sender, EventArgs e)
		{
			MoveCount--;
			MovesCB.RemoveText(game.Moves.Count + 1);
			UpdateMoveNavigationControls();
		}

		private String GetTurningPlayerString()
		{
			String str = String.Empty;
			str += game.CurPlayer == game.BlackPlayer ? "Black " : "White ";
			if (game.CurPlayer.Name.Length != 0)
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
				if (MoveCount > 0)
					MovesCB.Active = (int)MoveCount;
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

		private String FormatTime(TimeSpan Time)
		{
			if (Time.Ticks <= 0)
			{
				return "∞";
			}
			
			StringBuilder s = new StringBuilder();
			
			if (Math.Abs(Time.Hours) > 0)
			{
				s.Append(Math.Abs(Time.Hours).ToString("D2"));
				
				s.Append(':');
			}
			
			s.Append(Math.Abs(Time.Minutes).ToString("D2"));
			
			s.Append(':');
			
			s.Append(Math.Abs(Time.Seconds).ToString("D2"));
			
			return s.ToString();
		}

		private bool Update_Times()
		{
			TimeSpan BlackTime, WhiteTime;
			
			game.GetRemainingTimes(out BlackTime, out WhiteTime);
			
			BlackTimeLbl.Text = FormatTime(BlackTime);
			WhiteTimeLbl.Text = FormatTime(WhiteTime);
			
			if (game.gameState == GameState.Playing)
			{
				return true;
			}

			else
			{
				return false;
			}
		}

		private void Quit()
		{
			if (game.BlackPlayerEngine != null)
			{
				game.BlackPlayerEngine.Dispose();
			}
			if (game.WhitePlayerEngine != null)
			{
				game.WhitePlayerEngine.Dispose();
			}
			
			Application.Quit();
		}

		private void HandleGamePositionChanged(object sender, EventArgs e)
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
				Update_Times();
				if (game.GameTime.Ticks > 0)
				{
					GLib.Timeout.Add(100, Update_Times);
				}
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

		protected virtual void OnAboutActionActivated(object sender, System.EventArgs e)
		{
			using (AboutDialog dialog = new AboutDialog())
			{

				Assembly asm = Assembly.GetExecutingAssembly();
			
				dialog.ProgramName = (asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute).Title;
			
				dialog.Version = asm.GetName().Version.ToString();
			
				dialog.Comments = (asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0] as AssemblyDescriptionAttribute).Description;
			
				dialog.Copyright = (asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0] as AssemblyCopyrightAttribute).Copyright;
			
				//TODO
				//dialog.License = license;
			
				dialog.Authors = authors;
			
				dialog.Run();
				dialog.Destroy();
			}
		}
		
		protected virtual void OnNewGameActionActivated(object sender, System.EventArgs e)
		{
			new NewGameDialog(game).Run();
		}
		
		protected virtual void OnOpenActionActivated(object sender, System.EventArgs e)
		{
			using (FileChooserDialog fd = new FileChooserDialog("Open shogi game", this, FileChooserAction.Open,
					Gtk.Stock.Cancel, ResponseType.Cancel,
					Gtk.Stock.Save, ResponseType.Accept))
			{
				FileFilter filter;
				
				using (filter = new FileFilter())
				{
					filter.Name = "All files";
					filter.AddPattern("*");
					fd.AddFilter(filter);
				}
				
				using (filter = new FileFilter())
				{
					filter.Name = "PSN files";
					filter.AddPattern("*.psn");
					fd.AddFilter(filter);
					fd.Filter = filter;
				}
				
				if (fd.Run() == (int)ResponseType.Accept)
				{
					System.Console.WriteLine("opening file: " + fd.Filename);
					
					try
					{
						using (System.IO.FileStream stream = new System.IO.FileStream(fd.Filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
						{
							SaveGame savegame = null;
							bool error = false;
							try
							{
								Shogiban.FileFormat.PSN.Open(out savegame, stream);
							}
							catch (Exception ex)
							{
								error = true;
								MessageDialog dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, false, "Parsing the file {0} failed.", fd.Filename);
								dialog.SecondaryText = ex.Message;
								dialog.Run();
								dialog.Destroy();
								Console.WriteLine("Parsing failed: " + ex.ToString());
							}
							finally
							{
								stream.Close();
							}
							
							//TODO just commented for debugging
							//if (!error)
							{
								game.LoadSaveGame(savegame);
							}
						}
					}
					catch (Exception ex)
					{
						MessageDialog dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, false, "Opening the file {0} failed.", fd.Filename);
						dialog.SecondaryText = ex.Message;
						dialog.Run();
						dialog.Destroy();
						Console.WriteLine("Loading failed: " + ex.ToString());
					}
				}
				
				fd.Destroy();
			}
		}

		protected virtual void OnSaveGameActionActivated(object sender, System.EventArgs e)
		{
			using (FileChooserDialog fd = new FileChooserDialog("Save shogi game", this, FileChooserAction.Save,
				                         				 Gtk.Stock.Cancel, ResponseType.Cancel,
		                                                 Gtk.Stock.Save, ResponseType.Accept))
			{
				fd.DoOverwriteConfirmation = true;
				FileFilter filter;

				using (filter = new FileFilter())
				{
					filter.Name = "All files";
					filter.AddPattern("*");
					fd.AddFilter(filter);
				}

				using (filter = new FileFilter())
				{
					filter.Name = "PSN files";
					filter.AddPattern("*.psn");
					fd.AddFilter(filter);
					fd.Filter = filter;
				}
			
				if (fd.Run() == (int)ResponseType.Accept)
				{
					System.Console.WriteLine("saving file: " + fd.Filename);
				
					try
					{
						using (System.IO.FileStream stream = new System.IO.FileStream(fd.Filename, System.IO.FileMode.Create))
						{
							Shogiban.FileFormat.PSN.Save(game, stream);
							stream.Close();
						}
					}
					catch (Exception ex)
					{
						MessageDialog dialog = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, false, "Saving the file {0} failed.", fd.Filename);
						dialog.SecondaryText = ex.Message;
						dialog.Run();
						dialog.Destroy();
						Console.WriteLine("Saving failed: " + ex.ToString());
					}
				}
			
				fd.Destroy();
			}
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

