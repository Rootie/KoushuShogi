// 
//  PSN.cs
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
using System.IO;
using System.Text;

namespace Shogiban.FileFormat
{
	public class PSN
	{
		public static void Save(Game game, Stream stream)
		{
			StringBuilder data = new StringBuilder();
			data.AppendLine("[Sente \"" + game.BlackPlayer.Name + "\"]");
			data.AppendLine("[Gote \"" + game.WhitePlayer.Name + "\"]");
			
			data.AppendLine();
			
			int MoveNr = 1;
			foreach (ExtendedMove ExMove in game.Moves)
			{
				data.Append(MoveNr + "." + ExMove.move.ToString() + " ");
				MoveNr++;
			}
			
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data.ToString());
			stream.Write(bytes, 0, bytes.Length);
		}

		public static void Open(out SaveGame savegame, Stream stream)
		{
			savegame = new SaveGame();
			savegame.StartingPosition = new Position();
			savegame.StartingPosition.SetDefaultPosition();
			
			StreamReader sr = new StreamReader(stream);
			
			bool InHeaderArea = true;
			
			while (!sr.EndOfStream)
			{
				int StartChar = sr.Read();
				//Console.WriteLine("StartChar: '" + (Char)StartChar + "' (" + StartChar + ")");
				
				if (StartChar == '[')
				{
					//Header field
					StringBuilder HeaderStrData = new StringBuilder();
					while (true)
					{
						StartChar = sr.Read();
						if (StartChar.Equals(']'))
						{
							String HeaderStr = HeaderStrData.ToString();
							int HeaderSepIdx = 0;
							while (!Char.IsWhiteSpace(HeaderStr[HeaderSepIdx]))
							{
								HeaderSepIdx++;
							}
							
							String HeaderName = HeaderStr.Substring(0, HeaderSepIdx).TrimStart().ToLower();
							
							int ValueStartIdx = HeaderSepIdx + 1;
							while (Char.IsWhiteSpace(HeaderStr[ValueStartIdx]))
							{
								ValueStartIdx++;
							}
							if (HeaderStr[ValueStartIdx] == '"')
								ValueStartIdx++;
							int ValueEndIdx = HeaderStr.Length - 1;
							while (Char.IsWhiteSpace(HeaderStr[ValueEndIdx]))
							{
								ValueEndIdx--;
							}
							if (HeaderStr[ValueEndIdx] == '"')
								ValueEndIdx--;
							String Value = HeaderStr.Substring(ValueStartIdx, ValueEndIdx - ValueStartIdx + 1);
							
							switch (HeaderName)
							{
								case "sente":
								case "black":
									savegame.BlackPlayer.Name = Value;
									break;
								case "gote":
								case "white":
									savegame.WhitePlayer.Name = Value;
									break;
								case "handicap":
									throw new Exception("Handicap games are not supported by the PSN Loader at the moment.");
									//break;
								default:
									break;
							}
							break;
						}
						else if (StartChar < 0)
						{
							//TODO warning: unexpected end of file
							break;
						}
						
						
						HeaderStrData.Append((char)StartChar);
					}
				}
				else if (StartChar == '{')
				{
					//TODO ignoring comments at the moment
					while (true)
					{
						StartChar = sr.Read();
						if (StartChar == '}')
						{
							//Comment
							if (InHeaderArea)
							{
								//add comment to game
							}
							else
							{
								//add comment to move
							}
							
							break;
						}
						else if (StartChar < 0)
						{
							//TODO warning: unexpected end of file
							break;
						}
					}
				}
				else if (System.Char.IsLetter((Char)StartChar))
				{
					//Move
					StringBuilder MoveStr = new StringBuilder();
					MoveStr.Append((Char)StartChar);
					while (true)
					{
						StartChar = sr.Read();
						if (System.Char.IsWhiteSpace((Char)StartChar) || StartChar < 0)
						{
							System.Console.WriteLine(MoveStr.ToString());
							
							if (MoveStr.Length < 1)
							{
								//invalid move
								//TODO better error handling
								return;
							}
							Move CurMove = new Move();
							
							if (MoveStr[MoveStr.Length - 1] == '+')
							{
								CurMove.promote = true;
							}
							
							int FromIdx = 0;
							if (MoveStr[1] == '*')
							{
								CurMove.OnHandPiece = CommonShogiNotationHelpers.GetPieceByName(MoveStr[0]);
							}
							else
							{
								if (!Char.IsDigit(MoveStr[0]))
									FromIdx = 1;
								
								CurMove.From.x = CommonShogiNotationHelpers.GetColByName(MoveStr[FromIdx]);
								CurMove.From.y = CommonShogiNotationHelpers.GetRowByName(MoveStr[FromIdx + 1]);
							}
							
							int ToIdx = FromIdx + 2;
							if (!Char.IsDigit(MoveStr[ToIdx]))
								ToIdx++;
							
							CurMove.To.x = CommonShogiNotationHelpers.GetColByName(MoveStr[ToIdx]);
							CurMove.To.y = CommonShogiNotationHelpers.GetRowByName(MoveStr[ToIdx + 1]);
							
							System.Console.WriteLine(CurMove.ToString());
							
							savegame.Moves.Add(CurMove);
							break;
						}
						
						MoveStr.Append((Char)StartChar);
					}
				}
				else if (StartChar < 0)
				{
					break;
				}
				else
				{
					//useless crap (at least for the parser) like move numbering, game result or whitespace
					//read to next block
					while (System.Char.IsWhiteSpace((Char)sr.Peek()))
					{
						StartChar = sr.Read();
						if (StartChar < 0)
							break;
					}
				}
			}
		}
	}
}

