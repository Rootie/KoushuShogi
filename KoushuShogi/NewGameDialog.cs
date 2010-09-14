// 
//  NewGameDialog.cs
//  
//  Author:
//       rootie <>
//  
//  Copyright (c) 2010 rootie
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
namespace Shogiban
{
	public partial class NewGameDialog : Gtk.Dialog
	{
		private Game game;
		public Position StartPosition;
		
		public Player BlackPlayerEngine;
		public Player WhitePlayerEngine;
		
		public NewGameDialog(Game game)
		{
			this.game = game;
			
			this.Build();
		
		}
		
		protected override void OnRealized()
		{
			base.OnRealized();
			
			BlackPlayerNameEntry.Text = game.BlackPlayer.Name;
			WhitePlayerNameEntry.Text = game.WhitePlayer.Name;
		}
		
		protected override bool OnDeleteEvent(Gdk.Event evnt)
		{
			Destroy();
			return base.OnDeleteEvent(evnt);
		}
		
		protected override void OnResponse(Gtk.ResponseType response_id)
		{
			if (response_id == Gtk.ResponseType.Ok)
			{
				Type BlackPlayerEngineType = typeof(LocalPlayer);
				Type WhitePlayerEngineType = typeof(LocalPlayer);
				
				game.EndGame();
			
				game.BlackPlayer.Name = BlackPlayerNameEntry.Text;
				game.WhitePlayer.Name = WhitePlayerNameEntry.Text;
								
				switch (BlackPlayerEngineCB.Active)
				{
				case 0:
					BlackPlayerEngineType = typeof(LocalPlayer);
					break;
				case 1:
					BlackPlayerEngineType = typeof(GnuShogiPlayer);
					break;
				default:
					break;
				}
				if (game.BlackPlayerEngine == null || BlackPlayerEngineType != game.BlackPlayerEngine.GetType())
				{
					if (game.BlackPlayerEngine != null)
					{
						game.BlackPlayerEngine.Dispose();
					}
					Type[] ArgTypes = new Type[] {  };
					game.BlackPlayerEngine = (IPlayerEngine)BlackPlayerEngineType.GetConstructor(ArgTypes).Invoke(null);
				}

				switch (WhitePlayerEngineCB.Active)
				{
				case 0:
					WhitePlayerEngineType = typeof(LocalPlayer);
					break;
				case 1:
					WhitePlayerEngineType = typeof(GnuShogiPlayer);
					break;
				default:
					break;
				}
				if (game.WhitePlayerEngine == null || WhitePlayerEngineType != game.WhitePlayerEngine.GetType())
				{
					if (game.WhitePlayerEngine != null)
					{
						game.WhitePlayerEngine.Dispose();
					}
					Type[] ArgTypes = new Type[] {  };
					game.WhitePlayerEngine = (IPlayerEngine)WhitePlayerEngineType.GetConstructor(ArgTypes).Invoke(null);
				}
				
				game.SetDefaultPosition();
				Player StartingPlayer = BlackRB.Active ? game.BlackPlayer : game.WhitePlayer;
				Console.WriteLine("OnResponse: starting game");
				game.StartGame(StartingPlayer);
			}
			
			Destroy();
			base.OnResponse (response_id);
		}
		
		protected virtual void OnCancelBtnClicked(object sender, System.EventArgs e)
		{
			Destroy();
		}
	}
}

