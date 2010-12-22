// 
//  ShogibanSVG.cs
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
using Cairo;
using Gtk;

namespace Shogiban
{
	[System.ComponentModel.ToolboxItem(true)]
	public class ShogibanSVG : Gtk.DrawingArea
	{
		//layout constants
		private const double FIELD_SIZE = 100;
		private const double FIELD_NAMING_SIZE = 40;
		private const double ON_HAND_AREA_WIDTH = FIELD_SIZE + 80;
		private const double PADDING = 10;
		
		//helper constants
		private static readonly double PLAYFIELD_SIZE = Game.BOARD_SIZE * FIELD_SIZE;
		private static readonly double ON_HAND_AREA_HEIGHT = FIELD_NAMING_SIZE + PLAYFIELD_SIZE + FIELD_NAMING_SIZE;
		
		//helper constants for hit testing
		private const double PLAYFIELD_X_START = PADDING + ON_HAND_AREA_WIDTH + PADDING + FIELD_NAMING_SIZE;
		private static readonly double PLAYFIELD_X_END   = PLAYFIELD_X_START + PLAYFIELD_SIZE;
		private const double PLAYFIELD_Y_START = PADDING + FIELD_NAMING_SIZE;
		private static readonly double PLAYFIELD_Y_END = PLAYFIELD_Y_START + PLAYFIELD_SIZE;
		private const double WHITE_ON_HAND_AREA_X_START = PADDING;
		private const double WHITE_ON_HAND_AREA_X_END   = WHITE_ON_HAND_AREA_X_START + ON_HAND_AREA_WIDTH;
		private const double WHITE_ON_HAND_AREA_Y_START = PADDING;
		private static readonly double WHITE_ON_HAND_AREA_Y_END   = WHITE_ON_HAND_AREA_Y_START + ON_HAND_AREA_HEIGHT;
		private static readonly double BLACK_ON_HAND_AREA_X_START = PLAYFIELD_X_END + FIELD_NAMING_SIZE + PADDING;
		private static readonly double BLACK_ON_HAND_AREA_X_END   = BLACK_ON_HAND_AREA_X_START + ON_HAND_AREA_WIDTH;
		private const double BLACK_ON_HAND_AREA_Y_START = PADDING;
		private static readonly double BLACK_ON_HAND_AREA_Y_END   = BLACK_ON_HAND_AREA_Y_START + ON_HAND_AREA_HEIGHT;
		
		private static readonly Char[] VerticalNamings = CommonShogiNotationHelpers.GetVerticalNamings();
		private static readonly Char[] HorizontalNamings = CommonShogiNotationHelpers.GetHorizontalNamings();

		private Color BoardColor         = new Color(168 / 255f, 103 / 255f,  54 / 255f);
		private Color BorderColor        = new Color(210 / 255f, 160 / 255f, 100 / 255f);
		private Color SelectedFieldColor = new Color(168 / 255f, 140 / 255f,  54 / 255f);
		private Color LastMoveFieldColor = new Color(115 / 255f,  70 / 255f,  37 / 255f);
		private Color PossibleMoveColor  = new Color(188 / 255f, 143 / 255f,  74 / 255f);
		
		private Rsvg.Handle[] PieceGraphics = new Rsvg.Handle[(int)PieceType.PIECE_TYPES_COUNT];
		private Rsvg.Handle GyokushouGraphic;
		
		private Game gi;
		private int CurMoveNr = -1;
		private double ScaleFactor = 1;
		
		public Position pos;
		public Game game
		{
			get { return gi; }
			set
			{
				gi = value;
				gi.PositionChanged += HandleGiPositionChanged;
				
				RedrawBoard();
			}
		}

		public ShogibanSVG ()
		{
			// Insert initialization code here.
			LoadGraphics();
			
			Events |= Gdk.EventMask.ButtonPressMask;
		}
		
		public void ShowMoveNr(int MoveNr)
		{
			CurMoveNr = MoveNr;
			
			if (gi != null)
			{
				if (CurMoveNr < 0)
				{
					Console.WriteLine("ShogibanSVG.ShowMoveNr: -1");
					pos = gi.Position;
				}
				else
				{
					Console.WriteLine("ShogibanSVG.ShowMoveNr: " + CurMoveNr);
					pos = gi.Moves[CurMoveNr].OriginalPosition;
				}
			}
			else
			{
				pos = null;
			}
			
			RedrawBoard();
		}
		
		//TODO remove these two variables
		double mouse_x = 0;
		double mouse_y = 0;
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			// Insert button press handling code here.
			mouse_x = evnt.X * (1 / ScaleFactor);
			mouse_y = evnt.Y * (1 / ScaleFactor);
			RedrawBoard();
			
			//don't accept any inputs when reviewing past positions
			if (CurMoveNr >= 0 || gi == null)
				return true;
			
			if (gi.localPlayerMoveState == LocalPlayerMoveState.PickPromotion)
			{
				double PromotionChoiceAreaStartX = (Game.BOARD_SIZE - gi.GetLocalPlayerMove().To.x - 1) * FIELD_SIZE - FIELD_SIZE / 2 + PLAYFIELD_X_START;
				double PromotionChoiceAreaStartY = gi.GetLocalPlayerMove().To.y * FIELD_SIZE + PLAYFIELD_Y_START;

				if (mouse_y >= PromotionChoiceAreaStartY && mouse_y <= PromotionChoiceAreaStartY + FIELD_SIZE)
				{
					if (mouse_x >= PromotionChoiceAreaStartX && mouse_x < PromotionChoiceAreaStartX + FIELD_SIZE)
					{
						gi.PromotionClicked(false);
					}
					else if (mouse_x >= PromotionChoiceAreaStartX + FIELD_SIZE && mouse_x <= PromotionChoiceAreaStartX + 2 * FIELD_SIZE)
					{
						gi.PromotionClicked(true);
					}
				}
			}
			else if (mouse_x >= PLAYFIELD_X_START && mouse_x <= PLAYFIELD_X_END
				&& mouse_y >= PLAYFIELD_Y_START && mouse_y <= PLAYFIELD_Y_END)
			{
				int x = Game.BOARD_SIZE - 1 - (int)((mouse_x - PLAYFIELD_X_START) / FIELD_SIZE);
				int y = (int)((mouse_y - PLAYFIELD_Y_START) / FIELD_SIZE);
				
				gi.FieldClicked(x, y);
			}
			else if (mouse_x >= BLACK_ON_HAND_AREA_X_START && mouse_x <= BLACK_ON_HAND_AREA_X_END
				&& mouse_y >= BLACK_ON_HAND_AREA_Y_START && mouse_y <= BLACK_ON_HAND_AREA_Y_END)
			{
				OnHandPieceClicked((int)((ON_HAND_AREA_HEIGHT - mouse_y - BLACK_ON_HAND_AREA_Y_START) / FIELD_SIZE), true);
			}
			else if (mouse_x >= WHITE_ON_HAND_AREA_X_START && mouse_x <= WHITE_ON_HAND_AREA_X_END
				&& mouse_y >= WHITE_ON_HAND_AREA_Y_START && mouse_y <= WHITE_ON_HAND_AREA_Y_END)
			{
				OnHandPieceClicked((int)((mouse_y - WHITE_ON_HAND_AREA_Y_START) / FIELD_SIZE), false);
			}
			
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			base.OnExposeEvent(evnt);
			// Insert drawing code here.
			
			Cairo.Context cr = Gdk.CairoHelper.Create(this.GdkWindow);
			
			try
			{
				cr.Scale(ScaleFactor, ScaleFactor);
				cr.Save();
				cr.Translate(PADDING, PADDING);
			
				if (pos == null)
				{
					pos = new Position();
				}

				cr.Save();
				DrawOnHandPieces(cr, pos, false);
				cr.Translate(ON_HAND_AREA_WIDTH + 9 * FIELD_SIZE + 2 * FIELD_NAMING_SIZE + 2 * PADDING, 0);
				DrawOnHandPieces(cr, pos, true);
				cr.Restore();
			
				cr.Save();
				cr.Translate(ON_HAND_AREA_WIDTH + PADDING, 0);
				DrawBoard(cr, pos);
				cr.Restore();
			
				cr.Restore();
			
				//mouse courser
				cr.MoveTo(mouse_x - 10, mouse_y - 10);
				cr.LineTo(mouse_x + 10, mouse_y + 10);
				cr.MoveTo(mouse_x - 10, mouse_y + 10);
				cr.LineTo(mouse_x + 10, mouse_y - 10);
				cr.LineWidth = 1;
				cr.Color = new Color(0, 0, 0);
				cr.Stroke();
			}
			finally
			{
				((IDisposable)cr.Target).Dispose();
				((IDisposable)cr).Dispose();
			}

			return true;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			// Insert layout code here.
			double HScale = Allocation.Width / (PLAYFIELD_SIZE + 2 * FIELD_NAMING_SIZE + 2 * PADDING + 2 * ON_HAND_AREA_WIDTH + 2 * PADDING);
			double VScale = Allocation.Height / (PLAYFIELD_SIZE + 2 * FIELD_NAMING_SIZE + 2 * PADDING);
			ScaleFactor = Math.Min(HScale, VScale);
		}

		protected override void OnSizeRequested(ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 100;
			requisition.Width = 100;
		}
				
		private void OnHandPieceClicked(int idx, bool BlackPlayer)
		{
			System.Console.WriteLine(String.Format("ShogibanSVG.Clicked on hand {0}", idx));
			
			if (((gi.CurPlayer == gi.BlackPlayer) ^ BlackPlayer))
			{
				return;
			}
			
			int cur_pos = 0;
			for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
			{
				if (pos.OnHandPieces[pos.CurrentPlayerNumber, i] > 0)
				{
					if (cur_pos == idx)
					{
						//piece type found
						gi.OnHandPieceClicked((PieceType)i);
						
						return;
					}
					else
					{
						cur_pos++;
					}
				}
			}
		}

		private void RedrawBoard()
		{
			this.QueueDraw();
			//this.GdkWindow.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), false);
		}
		
		//TODO move this method to some more general place
		public String GetDataFile(params String[] FileNameParts)
		{
			if (FileNameParts.Length < 1)
			{
				throw new ArgumentException("At least one element is needed.", "FileNameParts");
			}
			
			String FilePath = String.Empty;
			foreach (String part in FileNameParts) {
				FilePath = System.IO.Path.Combine(FilePath, part);
			}
			
			String FullPath = String.Empty;
#if DEBUG
			FullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("../../data", FilePath));
			if (System.IO.File.Exists(FullPath))
				return FullPath;
#endif
			//TODO get correct data path (g_get_system_data_dirs)
			FullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), FilePath));
			if (System.IO.File.Exists(FullPath))
				return FullPath;
			
			
			throw new System.IO.FileNotFoundException("Datafile not found.", FilePath);
		}
		
		private void LoadGraphics()
		{
			
			String PiecesPath = "Pieces";
			
			PieceGraphics[1] = new Rsvg.Handle(GetDataFile(PiecesPath, "fu.svg"));
			PieceGraphics[2] = new Rsvg.Handle(GetDataFile(PiecesPath, "tokin.svg"));
			PieceGraphics[3] = new Rsvg.Handle(GetDataFile(PiecesPath, "kyousha.svg"));
			PieceGraphics[4] = new Rsvg.Handle(GetDataFile(PiecesPath, "narikyou.svg"));
			PieceGraphics[5] = new Rsvg.Handle(GetDataFile(PiecesPath, "keima.svg"));
			PieceGraphics[6] = new Rsvg.Handle(GetDataFile(PiecesPath, "narikei.svg"));
			PieceGraphics[7] = new Rsvg.Handle(GetDataFile(PiecesPath, "gin.svg"));
			PieceGraphics[8] = new Rsvg.Handle(GetDataFile(PiecesPath, "narigin.svg"));
			PieceGraphics[9] = new Rsvg.Handle(GetDataFile(PiecesPath, "kin.svg"));
			PieceGraphics[10] = new Rsvg.Handle(GetDataFile(PiecesPath, "kakugyou.svg"));
			PieceGraphics[11] = new Rsvg.Handle(GetDataFile(PiecesPath, "ryuuma.svg"));
			PieceGraphics[12] = new Rsvg.Handle(GetDataFile(PiecesPath, "hisha.svg"));
			PieceGraphics[13] = new Rsvg.Handle(GetDataFile(PiecesPath, "ryuuou.svg"));
			PieceGraphics[14] = new Rsvg.Handle(GetDataFile(PiecesPath, "ou.svg"));
			GyokushouGraphic = new Rsvg.Handle(GetDataFile(PiecesPath, "gyokushou.svg"));
			
			//TODO check if pieces are correctly loaded
		}
		
		private void DrawBoard(Context cr, Position pos)
		{
			Console.WriteLine("ShogibanSVG.DrawBoard");
			#region board border
			//Top
			cr.Rectangle(0, 0, Game.BOARD_SIZE * FIELD_SIZE + 2*FIELD_NAMING_SIZE, FIELD_NAMING_SIZE);
			//Left
			cr.Rectangle(0, 0, FIELD_NAMING_SIZE, Game.BOARD_SIZE * FIELD_SIZE + 2*FIELD_NAMING_SIZE); 
			//Bottom
			cr.Rectangle(0, Game.BOARD_SIZE * FIELD_SIZE + FIELD_NAMING_SIZE, Game.BOARD_SIZE * FIELD_SIZE + 2*FIELD_NAMING_SIZE, FIELD_NAMING_SIZE);
			//Right
			cr.Rectangle(9*FIELD_SIZE + FIELD_NAMING_SIZE, 0, FIELD_NAMING_SIZE, Game.BOARD_SIZE * FIELD_SIZE + 2*FIELD_NAMING_SIZE);
			cr.Color = BorderColor;
			cr.Fill();
			#endregion
			
			#region field namings
			cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal);
			cr.SetFontSize(FIELD_NAMING_SIZE * 0.9);
			cr.Color = new Color(0, 0, 0);
			
			for (int i = 0; i < VerticalNamings.Length; i++)
			{
				TextExtents extents = cr.TextExtents(VerticalNamings[i].ToString());
				double x = (FIELD_NAMING_SIZE/2) - (extents.Width/2 + extents.XBearing);
				double y = (FIELD_NAMING_SIZE + i*FIELD_SIZE + FIELD_SIZE/2) - (extents.Height/2 + extents.YBearing);
				
				cr.MoveTo(x, y);
				cr.ShowText(VerticalNamings[i].ToString());
				cr.MoveTo(x + 9*FIELD_SIZE + FIELD_NAMING_SIZE, y);
				cr.ShowText(VerticalNamings[i].ToString());
			}
			for (int i = 0; i < HorizontalNamings.Length; i++)
			{
				TextExtents extents = cr.TextExtents(HorizontalNamings[i].ToString());
				double x = (FIELD_NAMING_SIZE + i*FIELD_SIZE + FIELD_SIZE/2) - (extents.Width/2 + extents.XBearing);
				double y = (FIELD_NAMING_SIZE/2) - (extents.Height/2 + extents.YBearing);
				
				cr.MoveTo(x, y);
				cr.ShowText(HorizontalNamings[Game.BOARD_SIZE-i-1].ToString());
				cr.MoveTo(x, y + 9*FIELD_SIZE + FIELD_NAMING_SIZE);
				cr.ShowText(HorizontalNamings[Game.BOARD_SIZE-i-1].ToString());
			}
			#endregion
			
			#region board playfield
			//background
			cr.Translate(FIELD_NAMING_SIZE, FIELD_NAMING_SIZE);
			cr.Rectangle(0, 0, Game.BOARD_SIZE * FIELD_SIZE, Game.BOARD_SIZE * FIELD_SIZE);
			cr.Color = BoardColor;
			cr.Fill();
			
			if (gi != null)
			{
				//highlight selected piece field
				if (gi.localPlayerMoveState != LocalPlayerMoveState.Wait
					&& gi.localPlayerMoveState != LocalPlayerMoveState.PickSource
					&& CurMoveNr < 0)
				{
					if (gi.GetLocalPlayerMove().OnHandPiece == PieceType.NONE)
					{
						cr.Rectangle((Game.BOARD_SIZE - gi.GetLocalPlayerMove().From.x - 1) * FIELD_SIZE, gi.GetLocalPlayerMove().From.y * FIELD_SIZE, FIELD_SIZE, FIELD_SIZE);
						cr.Color = SelectedFieldColor;
						cr.Fill();
					}
				}
		
				//highlight last move
				if (gi.Moves.Count > 0 && CurMoveNr != 0)
				{
					Move move;
					if (CurMoveNr < 0)
						move = gi.Moves[gi.Moves.Count-1].move;
					else
						move = gi.Moves[CurMoveNr - 1].move;
					
					if (move.OnHandPiece == PieceType.NONE)
					{
						cr.Rectangle((Game.BOARD_SIZE - move.From.x - 1) * FIELD_SIZE, move.From.y * FIELD_SIZE, FIELD_SIZE, FIELD_SIZE);
					}
					cr.Rectangle((Game.BOARD_SIZE - move.To.x - 1) * FIELD_SIZE, move.To.y * FIELD_SIZE, FIELD_SIZE, FIELD_SIZE);
					cr.Color = LastMoveFieldColor;
					cr.Fill();
				}
				
				//highlight possible moves
				if (gi.localPlayerMoveState == LocalPlayerMoveState.PickDestination && CurMoveNr < 0)
				{
					ValidMoves Moves;
					Move LocalPlayerMove = gi.GetLocalPlayerMove();
					if (LocalPlayerMove.OnHandPiece == PieceType.NONE)
					{
						Moves = pos.GetValidBoardMoves(LocalPlayerMove.From);
					}
					else
					{
						Moves = pos.GetValidOnHandMoves(LocalPlayerMove.OnHandPiece, gi.CurPlayer == gi.BlackPlayer);
					}
					
					foreach (BoardField Field in Moves)
					{
						cr.Rectangle((Game.BOARD_SIZE - Field.x - 1) * FIELD_SIZE, Field.y * FIELD_SIZE, FIELD_SIZE, FIELD_SIZE);
						cr.Color = PossibleMoveColor;
						cr.Fill();
					}
				}
			}
			
			//dividing lines
			for (int i = 0; i <= Game.BOARD_SIZE; i++)
			{
				cr.MoveTo(0, i * FIELD_SIZE);
				cr.LineTo(Game.BOARD_SIZE * FIELD_SIZE, i * FIELD_SIZE);
			}
			for (int i = 0; i <= Game.BOARD_SIZE; i++)
			{
				cr.MoveTo(i * FIELD_SIZE, 0);
				cr.LineTo(i * FIELD_SIZE, Game.BOARD_SIZE * FIELD_SIZE);
			}
			cr.Color = new Color(0, 0, 0);
			cr.LineWidth = 2.5;
			cr.LineCap = LineCap.Round;
			cr.Stroke();
			#endregion
	
			DrawPieces(cr, pos);
			
			//draw promotion choice area
			if (gi != null && gi.localPlayerMoveState == LocalPlayerMoveState.PickPromotion && CurMoveNr < 0)
			{
				double PromotionChoiceAreaStartX = (Game.BOARD_SIZE - gi.GetLocalPlayerMove().To.x - 1) * FIELD_SIZE - FIELD_SIZE / 2;
				double PromotionChoiceAreaStartY = gi.GetLocalPlayerMove().To.y * FIELD_SIZE;
				
				cr.Save();
				//draw boarder
				cr.Translate(PromotionChoiceAreaStartX - PADDING,
					PromotionChoiceAreaStartY - PADDING);
				cr.Rectangle(0, 0, 2 * FIELD_SIZE + 2 * PADDING, FIELD_SIZE + 2 * PADDING);
				cr.Color = new Color(0, 0, 0);
				cr.Fill();
				
				cr.Translate(PADDING, PADDING);
				cr.Rectangle(0, 0, 2 * FIELD_SIZE, FIELD_SIZE);
				cr.Color = BoardColor;
				cr.Fill();
				
				Move LocalPlayerMove = gi.GetLocalPlayerMove();
				FieldInfo From = pos.Board[LocalPlayerMove.From.x, LocalPlayerMove.From.y];
				DrawPiece(cr, From.Piece, From.Direction, 0, 0);
				DrawPiece(cr, From.Piece.GetPromotedPiece(), From.Direction, FIELD_SIZE, 0);
				
				cr.Restore();	
			}
		}

		private void DrawPieces(Context cr, Position pos)
		{
			for (int x = 0; x < Game.BOARD_SIZE; x++)
				for (int y = 0; y < Game.BOARD_SIZE; y++)
				{
					DrawPiece(cr, pos.Board[x,y].Piece, pos.Board[x, y].Direction, (Game.BOARD_SIZE - x - 1) * FIELD_SIZE, y * FIELD_SIZE);
				}
		}
		
		private void DrawOnHandPieces(Context cr, Position pos, bool BlackPlayer)
		{
			cr.Save();
			cr.Rectangle(0, 0, ON_HAND_AREA_WIDTH, 9 * FIELD_SIZE + 2 * FIELD_NAMING_SIZE);
			cr.Color = BorderColor;
			cr.FillPreserve();
			cr.Color = new Color(0.6, 0.5, 0.55);
			cr.LineWidth = 3;
			cr.Stroke();
			
			if (BlackPlayer)
			{
				cr.Translate(0, 8 * FIELD_SIZE + 2 * FIELD_NAMING_SIZE);
			}
			
			for (int i = 0; i < (int)PieceType.PIECE_TYPES_COUNT; i++)
			{
				int player_nr = BlackPlayer ? 0 : 1;
				if (pos.OnHandPieces[player_nr, i] != 0)
				{
					//highlight selected piece
					if (gi != null && gi.localPlayerMoveState != LocalPlayerMoveState.Wait
						&& gi.localPlayerMoveState != LocalPlayerMoveState.PickSource
						&& gi.GetLocalPlayerMove().OnHandPiece != PieceType.NONE
						&& gi.GetLocalPlayerMove().OnHandPiece == (PieceType)i
						&& !((gi.CurPlayer == gi.BlackPlayer) ^ BlackPlayer))
					{
						cr.Rectangle(0, 0, FIELD_SIZE, FIELD_SIZE);
						cr.Color = new Color(0.8, 0.835, 0.4);
						cr.Fill();
					}
					
					//draw piece
					cr.Save();
					if (!BlackPlayer)
					{
						cr.Rotate(180 * Math.PI / 180);
						cr.Translate(-FIELD_SIZE, -FIELD_SIZE);
					}
					cr.Scale(FIELD_SIZE / PieceGraphics[i].Dimensions.Width, FIELD_SIZE / PieceGraphics[i].Dimensions.Width);
					PieceGraphics[i].RenderCairo(cr);
					cr.Restore();
					
					//draw amount
					cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal);
					cr.SetFontSize(FIELD_SIZE / 3 * 0.9);
					cr.Color = new Color(0, 0, 0);
	
					String amount_str = "x " + pos.OnHandPieces[player_nr, i].ToString();
					TextExtents extents = cr.TextExtents(amount_str);
					double x = (FIELD_SIZE);
					// - (extents.Width/2 + extents.XBearing);
					double y = (FIELD_SIZE / 2) - (extents.Height / 2 + extents.YBearing);
					
					cr.MoveTo(x, y);
					cr.ShowText(amount_str);
					
					double offset = BlackPlayer ? -FIELD_SIZE - PADDING : FIELD_SIZE + PADDING;
					cr.Translate(0, offset);
				}
			}
			cr.Restore();
		}
		
		private void DrawPiece(Context cr, PieceType Piece, PieceDirection Direction, double x, double y)
		{
			int idx = (int)Piece;
			if (idx < (int)PieceType.FUHYOU || idx > (int)PieceType.OUSHOU)
				return;
			
			cr.Save();
			cr.Translate(x, y);
			if (Direction == PieceDirection.DOWN)
			{
				cr.Rotate(180 * Math.PI / 180);
				cr.Translate(-FIELD_SIZE,-FIELD_SIZE);
			}
			cr.Scale(FIELD_SIZE/PieceGraphics[idx].Dimensions.Width, FIELD_SIZE/PieceGraphics[idx].Dimensions.Width);
			if (idx == (int)PieceType.OUSHOU && Direction == PieceDirection.DOWN)
			{
				GyokushouGraphic.RenderCairo(cr);
			}
			else
			{
				PieceGraphics[idx].RenderCairo(cr);
			}
			
			cr.Restore();
		}

		private void HandleGiPositionChanged(object sender, EventArgs e)
		{
			//Gtk.Application.Invoke(delegate
			//{
			if (CurMoveNr < 0)
			{
				Console.WriteLine("ShogibanSVG.HandleGiPositionChanged: ThreadID: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
				pos = gi.Position;
				pos.DebugPrint();

				}
				
				RedrawBoard();
				Console.WriteLine("ShogibanSVG.HandleGiPositionChanged: leave");
			//});
		}
	}
}
