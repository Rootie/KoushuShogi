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
			Position CurPosition = new Position();
			CurPosition.SetDefaultPosition();
			
			BoardField LastTarget = new BoardField(-1, -1);
			
			StreamReader sr = new StreamReader(stream);
			
			bool InHeaderArea = true;
			
			while (!sr.EndOfStream)
			{
				int CurChar = sr.Read();
				
				if (CurChar == '[')
				{
					//Header field
					StringBuilder HeaderStrData = new StringBuilder();
					while (true)
					{
						CurChar = sr.Read();
						if (CurChar.Equals(']'))
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
									Handicaps Handicap = Handicaps.None;
									switch (Value.ToLower())
									{
										case "even":
											break;
										case "lance":
											Handicap = Handicaps.Lance;
											break;
										case "bishop":
											Handicap = Handicaps.Bishop;
											break;
										case "rook":
											Handicap = Handicaps.Rook;
											break;
										case "rook and lance":
										case "rook & lance":
										case "rook + lance":
										case "rook+lance":
											Handicap = Handicaps.RookAndLance;
											break;
										case "two piece":
										case "two pieces":
										case "rook and bishop":
										case "rook & bishop":
										case "rook + bishop":
										case "rook+bishop":
											Handicap = Handicaps.TwoPiece;
											break;
										case "four piece":
										case "four pieces":
											Handicap = Handicaps.FourPiece;
											break;
										case "six piece":
										case "six pieces":
											Handicap = Handicaps.SixPiece;
											break;
										case "eight piece":
										case "eight pieces":
											Handicap = Handicaps.EightPiece;
											break;
										default:
											throw new Exception("Unknown handicap: " + Value);
										//break;
									}
									savegame.StartingPosition.SetHandicapPosition(Handicap);
									CurPosition.SetHandicapPosition(Handicap);
									break;
								default:
									break;
							}
							break;
						}
						else if (CurChar < 0)
						{
							//TODO warning: unexpected end of file
							break;
						}
						
						
						HeaderStrData.Append((char)CurChar);
					}
				}
				else if (CurChar == '{')
				{
					//TODO ignoring comments at the moment
					while (true)
					{
						CurChar = sr.Read();
						if (CurChar == '}')
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

						else if (CurChar < 0)
						{
							//TODO warning: unexpected end of file
							break;
						}
					}
				}
				else if (CurChar == '(')
				{
					int Count = 0;
					//TODO ignoring variations at the moment
					while (true)
					{
						CurChar = sr.Read();
						if (CurChar == ')')
						{
							if (Count == 0)
								break;
							
							Count--;
						}
						else if (CurChar == '(')
						{
							Count++;
						}
						else if (CurChar < 0)
						{
							//TODO warning: unexpected end of file
							break;
						}
					}
				}
				else if (Char.IsDigit((Char)CurChar) || Char.IsLetter((Char)CurChar) || CurChar == '+')
				{
					//Move
					StringBuilder MoveStrData = new StringBuilder();
					MoveStrData.Append((Char)CurChar);
					while (true)
					{
						CurChar = sr.Read();
						if (System.Char.IsWhiteSpace((Char)CurChar) || CurChar < 0)
						{
							String MoveStr = MoveStrData.ToString();
							System.Console.WriteLine(" -----------> Parsing move: " + MoveStr + " <-----------");
							
							int CurIdx = MoveStr.IndexOf('.') + 1;
							if ((MoveStr.Length - CurIdx) < 1)
							{
								break;
							}
							if ((MoveStr.Length - CurIdx) > 9)
							{
								//invalid move
								//TODO better error handling
								throw new Exception("Invalid move: move too long: " + MoveStr);
							}
							
							if (MoveStr.EndsWith("..."))
							{
								savegame.StartingPosition.CurPlayer = PieceDirection.DOWN;
								CurPosition.CurPlayer = PieceDirection.DOWN;
								break;
							}
							if (MoveStr.EndsWith("0-1") || MoveStr.EndsWith("1-0"))
							{
								//game result
								//just ignoring it
								break;
							}
							
							Move CurMove = new Move();
							
							if (MoveStr[MoveStr.Length - 1] == '+')
							{
								CurMove.promote = true;
							}
							
							PieceType SourcePieceType = PieceType.NONE;
							PieceType TargetPieceType = PieceType.NONE;
							CurMove.From.x = -1;
							CurMove.From.y = -1;
							CurMove.To.x = -1;
							CurMove.To.y = -1;
							bool SourceMayBeTarget = true;
							bool IsHittingEnemyPiece = false;
							bool FoundSourceParts = false;
							bool FoundTargetParts = false;
							
							if (MoveStr.Length > 3 && MoveStr[CurIdx + 1] == '*')
							{
								CurMove.OnHandPiece = CommonShogiNotationHelpers.GetPieceByName(MoveStr[CurIdx]);
								SourceMayBeTarget = false;
								CurIdx += 2;
								FoundSourceParts = true;
							}
							else
							{
								bool SoureIsPromoted = false;
								if (MoveStr.Length > CurIdx && MoveStr[CurIdx] == '+')
								{
									CurIdx++;
									SoureIsPromoted = true;
								}
								if (MoveStr.Length > CurIdx)
								{
									try
									{
										SourcePieceType = CommonShogiNotationHelpers.GetPieceByName(MoveStr[CurIdx]);
										if (SoureIsPromoted)
										{
											SourcePieceType = SourcePieceType.GetPromotedPiece();
										}
										CurIdx++;
										FoundSourceParts = true;
									}
									catch
									{
										if (SoureIsPromoted)
										{
											throw new Exception("Invalid format: Expected source piece type: " + MoveStr);
										}
									}
								}
								
								if (MoveStr.Length > CurIdx)
								{
									try
									{
										CurMove.From.x = CommonShogiNotationHelpers.GetColByName(MoveStr[CurIdx]);
										CurIdx++;
										FoundSourceParts = true;
									}
									catch
									{
									}
								}
								
								if (MoveStr.Length > CurIdx)
								{
									try
									{
										CurMove.From.y = CommonShogiNotationHelpers.GetRowByName(MoveStr[CurIdx]);
										CurIdx++;
										FoundSourceParts = true;
									}
									catch
									{
									}
								}
							}
							
							if (MoveStr.Length > CurIdx)
							{
								if (MoveStr[CurIdx] == 'x')
								{
									IsHittingEnemyPiece = true;
									SourceMayBeTarget = false;
									CurIdx++;
								}
								
								if (MoveStr[CurIdx] == '-')
								{
									SourceMayBeTarget = false;
									CurIdx++;
								}
							}
							
							bool TargetIsPromoted = false;
							if (MoveStr.Length > CurIdx && MoveStr[CurIdx] == '+')
							{
								CurIdx++;
								TargetIsPromoted = true;
							}
							if (MoveStr.Length > CurIdx)
							{
								try
								{
									TargetPieceType = CommonShogiNotationHelpers.GetPieceByName(MoveStr[CurIdx]);
									if (TargetIsPromoted)
									{
										TargetPieceType = TargetPieceType.GetPromotedPiece();
									}
									IsHittingEnemyPiece = true;

									SourceMayBeTarget = false;
									CurIdx++;
									FoundTargetParts = true;
								}
								catch
								{
									if (TargetIsPromoted)
									{
										throw new Exception("Invalid format: Expected target piece type: " + MoveStr);
									}
								}
							}
							
							if (MoveStr.Length > CurIdx)
							{
								try
								{
									CurMove.To.x = CommonShogiNotationHelpers.GetColByName(MoveStr[CurIdx]);
									SourceMayBeTarget = false;
									CurIdx++;
									FoundTargetParts = true;
								}
								catch
								{
								}
							}
							
							if (MoveStr.Length > CurIdx)
							{
								try
								{
									CurMove.To.y = CommonShogiNotationHelpers.GetRowByName(MoveStr[CurIdx]);
									SourceMayBeTarget = false;
									CurIdx++;
									FoundTargetParts = true;
								}
								catch
								{
								}
							}
							
							if (CurMove.OnHandPiece != PieceType.NONE)
							{
								if (CurMove.To.x < 0 && CurMove.To.y < 0)
									throw new Exception("Invalid Move: No target defined in move: " + MoveStr);
								
								if (CurMove.To.x < 0 || CurMove.To.y < 0)
								{
									ValidMoves PossibleMoves = CurPosition.GetValidOnHandMoves(CurMove.OnHandPiece);
									ValidMoves FoundMoves = new ValidMoves();
									
									foreach (BoardField PossibleMove in PossibleMoves)
									{
										if (CurMove.To.x != -1)
										{
											if (PossibleMove.x != CurMove.To.x)
											{
												continue;
											}
										}
										if (CurMove.To.y != -1)
										{
											if (PossibleMove.y != CurMove.To.y)
											{
												continue;
											}
										}
										
										FoundMoves.Add(PossibleMove);
									}
									
									if (FoundMoves.Count == 1)
									{
										CurMove.To = FoundMoves[0];
									}
									else if (FoundMoves.Count == 0)
										throw new Exception("Invalid Move: No valid move found for drop: " + MoveStr);
									else
									{
										StringBuilder Message = new StringBuilder();
										Message.Append("Invalid Move: Ambiguous drop: ");
										Message.AppendLine(MoveStr);
										Message.AppendLine();
										Message.AppendLine("Found targets:");
										foreach (BoardField AmbiguousMove in FoundMoves)
										{
											Message.AppendLine(AmbiguousMove.ToString());
										}
										throw new Exception(Message.ToString());
									}
								}
							}
							else
							{
								System.Collections.Generic.List<Move> FoundMoves = new System.Collections.Generic.List<Move>();
								System.Collections.Generic.List<Move> FoundPreferredMoves = new System.Collections.Generic.List<Move>();
								
								for (int x = 0; x < Game.BOARD_SIZE; x++)
								{
									for (int y = 0; y < Game.BOARD_SIZE; y++)
									{
										if (CurPosition.Board[x, y].Direction != CurPosition.CurPlayer)
											continue;
										
										ValidMoves PossibleMoves = CurPosition.GetValidBoardMoves(new BoardField(x, y));
										
										foreach (BoardField PossibleMove in PossibleMoves)
										{
											if (IsHittingEnemyPiece
												&& PossibleMove == LastTarget
												&& (FoundSourceParts && !FoundTargetParts || !FoundSourceParts && FoundTargetParts))
											{
												PieceType Piece;
												int Move_x;
												int Move_y;
												if (FoundSourceParts)
												{
													Piece = SourcePieceType;
													Move_x = CurMove.From.x;
													Move_y = CurMove.From.y;
												}
												else
												{
													Piece = TargetPieceType;
													Move_x = CurMove.To.x;
													Move_y = CurMove.To.y;
												}
												
												bool FoundPreferredMove = true;
												
												if (Piece != PieceType.NONE && CurPosition.Board[x, y].Piece != Piece)
													FoundPreferredMove = false;
												if (Move_x != -1 && x != Move_x
													|| Move_y != -1 &&  y != Move_y)
													FoundPreferredMove = false;
												
												if (FoundPreferredMove)
												{
													Move PreferredMove = new Move();
													PreferredMove.From.x = x;
													PreferredMove.From.y = y;
													PreferredMove.To = PossibleMove;
													FoundPreferredMoves.Add(PreferredMove);
												}
											}
											
											//if we have found a preferred move we don't need to search for normal move anymore
											if (FoundPreferredMoves.Count != 0)
												continue;
											
											if (SourceMayBeTarget
												&& CurMove.From.x != -1
												&& CurMove.From.y != -1)
											{
												if ((x != CurMove.From.x || y != CurMove.From.y)
													&& (CurMove.From.x != PossibleMove.x || CurMove.From.y != PossibleMove.y))
												{
													continue;
												}
											
											}
											else
											{
												if (CurMove.From.x != -1)
												{
													if (!SourceMayBeTarget && x != CurMove.From.x
														|| x != CurMove.From.x && CurMove.From.x != PossibleMove.x)
													{
														continue;
													}
												}
												if (CurMove.From.y != -1)
												{
													if (!SourceMayBeTarget && y != CurMove.From.y
														|| y != CurMove.From.y && CurMove.From.y != PossibleMove.y)
													{
														continue;
													}
												}
											}
											if (CurMove.From.x != -1)
											{
												if (!SourceMayBeTarget && x != CurMove.From.x
													|| x != CurMove.From.x && CurMove.From.x != PossibleMove.x)
												{
													continue;
												}
											}
											if (CurMove.From.y != -1)
											{
												if (!SourceMayBeTarget && y != CurMove.From.y
													|| y != CurMove.From.y && CurMove.From.y != PossibleMove.y)
												{
													continue;
												}
											}
											if (SourcePieceType != PieceType.NONE)
											{
												if (!SourceMayBeTarget && CurPosition.Board[x, y].Piece != SourcePieceType
													|| CurPosition.Board[x, y].Piece != SourcePieceType && CurPosition.Board[PossibleMove.x, PossibleMove.y].Piece != SourcePieceType)
												{
													continue;
												}
											}
											
											if (CurMove.To.x != -1)
											{
												if (PossibleMove.x != CurMove.To.x)
												{
													continue;
												}
											}
											if (CurMove.To.y != -1)
											{
												if (PossibleMove.y != CurMove.To.y)
												{
													continue;
												}
											}
											if (TargetPieceType != PieceType.NONE)
											{
												if (CurPosition.Board[PossibleMove.x, PossibleMove.y].Piece != TargetPieceType)
												{
													continue;
												}
											}
											if (IsHittingEnemyPiece)
											{
												if (CurPosition.Board[PossibleMove.x, PossibleMove.y].Piece == PieceType.NONE)
												{
													continue;
												}
											}
											
											Move FoundMove = new Move();
											FoundMove.From.x = x;
											FoundMove.From.y = y;
											FoundMove.To = PossibleMove;
											FoundMoves.Add(FoundMove);
										}
									}
								}
								
								if (FoundPreferredMoves.Count == 1)
								{
									CurMove.From = FoundPreferredMoves[0].From;
									CurMove.To = FoundPreferredMoves[0].To;
								}
								else if (FoundPreferredMoves.Count > 1)
								{
									StringBuilder Message = new StringBuilder();
									Message.Append("Invalid Move: Ambiguous move: ");
									Message.AppendLine(MoveStr);
									Message.AppendLine();
									Message.AppendLine("Found preferred moves:");
									foreach (Move AmbiguousMove in FoundPreferredMoves)
									{
										Message.AppendLine(AmbiguousMove.ToString());
									}
									throw new Exception(Message.ToString());
								}
								else if (FoundMoves.Count == 1)
								{
									CurMove.From = FoundMoves[0].From;
									CurMove.To = FoundMoves[0].To;
								}
								else if (FoundMoves.Count == 0)
									throw new Exception("Invalid Move: No valid move found: " + MoveStr);
								else
								{
									StringBuilder Message = new StringBuilder();
									Message.Append("Invalid Move: Ambiguous move: ");
									Message.AppendLine(MoveStr);
									Message.AppendLine();
									Message.AppendLine("Found moves:");
									foreach (Move AmbiguousMove in FoundMoves)
									{
										Message.AppendLine(AmbiguousMove.ToString());
									}
									throw new Exception(Message.ToString());
								}

							}
							
							//System.Console.WriteLine(CurMove.ToString());
							CurPosition.ApplyMove(CurMove);
							//CurPosition.DebugPrint();
							savegame.Moves.Add(CurMove);
							LastTarget = CurMove.To;
							break;
						}
						
						MoveStrData.Append((Char)CurChar);
					}
				}
				else if (Char.IsWhiteSpace((Char)CurChar))
				{
					while (Char.IsWhiteSpace((Char)sr.Peek()))
					{
						CurChar = sr.Read();
						if (CurChar < 0)
							break;
					}
				}
				else if (CurChar < 0)
				{
					break;
				}
				else
				{
					throw new Exception("Invalid Format: Unrecognized character '" + (char)CurChar + "'");
				}
			}
		}
	}
}

