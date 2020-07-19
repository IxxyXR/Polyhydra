using System;
using System.Collections.Generic;
using System.Linq;
using Conway;
using RtMidi.LowLevel;
using UnityEngine;


public class PolyMidi : MonoBehaviour
{

   public PolyHydra poly;
   public int UpdateSliderEvery = 3;
   public AppearancePresets aPresets;

   private int LastFrameRendered = -1;
   private MidiProbe _probe;
   private MidiOutPort AkaiOutPort;
   private MidiInPort AkaiInPort;
   private MidiOutPort NovationOutPort;
   private MidiInPort NovationInPort;
   private int[] MidiColorValues = {1, 5, 3};
   private const int MAXOPS = 4  ;
   private AkaiPrefabController akaiPrefab;
   private NovationPrefabController novationPrefab;
   private float SliderDeadZone = 0.05f;
   private byte[][] LastSliderValue;


   private int currentControl;
   private float currentControlValue;
   private bool ControlHasChanged;

   private enum controlBanks
   {
      AkaiSlider,
      NovationSlider,
      NovationDialSendA,
      NovationDialSendB,
      NovationDialPan,
   }

   private controlBanks currentControlBank;

   private List<(int, PolyHydraEnums.JohnsonPolyTypes)> Johnsons = new List<(int, PolyHydraEnums.JohnsonPolyTypes)>
   {
      (3, PolyHydraEnums.JohnsonPolyTypes.Prism),
      (5, PolyHydraEnums.JohnsonPolyTypes.Prism),
      (6, PolyHydraEnums.JohnsonPolyTypes.Prism),
//      (7, PolyHydraEnums.JohnsonPolyTypes.Prism),
      (8, PolyHydraEnums.JohnsonPolyTypes.Prism),

//      (3, PolyHydraEnums.JohnsonPolyTypes.Antiprism),
      (4, PolyHydraEnums.JohnsonPolyTypes.Antiprism),
      (5, PolyHydraEnums.JohnsonPolyTypes.Antiprism),
      (6, PolyHydraEnums.JohnsonPolyTypes.Antiprism),
//      (7, PolyHydraEnums.JohnsonPolyTypes.Antiprism),
//      (8, PolyHydraEnums.JohnsonPolyTypes.Antiprism),

//      (4, PolyHydraEnums.JohnsonPolyTypes.Pyramid),
//      (5, PolyHydraEnums.JohnsonPolyTypes.Pyramid),
//      (6, PolyHydraEnums.JohnsonPolyTypes.Pyramid),
////      (7, PolyHydraEnums.JohnsonPolyTypes.Pyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.Pyramid),

//      (3, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),
//      (4, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),
//      (5, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),
//      (6, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),
//      (7, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.ElongatedPyramid),

//      (3, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),
//      (4, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),
//      (5, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),
//      (6, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),
//      (7, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedPyramid),

      (3, PolyHydraEnums.JohnsonPolyTypes.Dipyramid),
      (5, PolyHydraEnums.JohnsonPolyTypes.Dipyramid),
      (6, PolyHydraEnums.JohnsonPolyTypes.Dipyramid),
//      (7, PolyHydraEnums.JohnsonPolyTypes.Dipyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.Dipyramid),

      (3, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),
      (4, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),
      (5, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),
      (6, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),
//      (7, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.ElongatedDipyramid),

      (3, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),
      (4, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (5, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),
      (6, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (7, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (8, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedDipyramid),

//      (3, PolyHydraEnums.JohnsonPolyTypes.Cupola),
//      (4, PolyHydraEnums.JohnsonPolyTypes.Cupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.Cupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.Cupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.Cupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.Cupola),

//      (3, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//      (4, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.ElongatedCupola),
//
//      (3, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),
//      (4, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedCupola),

      (3, PolyHydraEnums.JohnsonPolyTypes.OrthoBicupola),
      (4, PolyHydraEnums.JohnsonPolyTypes.OrthoBicupola),
      (3, PolyHydraEnums.JohnsonPolyTypes.GyroBicupola),
      (4, PolyHydraEnums.JohnsonPolyTypes.GyroBicupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.Bicupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.Bicupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.Bicupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.Bicupola),

//      (3, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),
//      (4, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.ElongatedBicupola),

//      (3, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),
//      (4, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),
//      (5, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),
//      (6, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),
//      (7, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),
//      (8, PolyHydraEnums.JohnsonPolyTypes.GyroelongatedBicupola),

      (3, PolyHydraEnums.JohnsonPolyTypes.Rotunda),
   };

   private PolyTypes[] Polys =
   {
      PolyTypes.Tetrahedron,
      PolyTypes.Truncated_Tetrahedron,
      PolyTypes.Cube,
      PolyTypes.Truncated_Cube,
      PolyTypes.Snub_Cube,
      PolyTypes.Octahedron,
      PolyTypes.Truncated_Octahedron,
      PolyTypes.Cuboctahedron,
      PolyTypes.Truncated_Cuboctahedron,
      PolyTypes.Rhombicuboctahedron,
      PolyTypes.Dodecahedron,
      PolyTypes.Truncated_Dodecahedron,
      PolyTypes.Snub_Dodecahedron,
      PolyTypes.Icosahedron,
      PolyTypes.Truncated_Icosahedron,
      PolyTypes.Icosidodecahedron,
      PolyTypes.Great_Dodecahedron,
   };

   // If an op supports "Existing" face selection mode use it, otherwise use "Alternate"
   private PolyHydraEnums.Ops[] OpsWithExistingFaceMode =
   {
//      PolyHydraEnums.Ops.Kis,
      PolyHydraEnums.Ops.Zip,
      PolyHydraEnums.Ops.Expand,
//      PolyHydraEnums.Ops.Join,
//      PolyHydraEnums.Ops.Needle,
//      PolyHydraEnums.Ops.Meta,
      PolyHydraEnums.Ops.Truncate,
//      PolyHydraEnums.Ops.Gyro,
      PolyHydraEnums.Ops.Loft,
      PolyHydraEnums.Ops.Chamfer,
      PolyHydraEnums.Ops.Quinto,
      PolyHydraEnums.Ops.Lace,
      PolyHydraEnums.Ops.OppositeLace,
      PolyHydraEnums.Ops.Stake,
      PolyHydraEnums.Ops.Propeller,
      PolyHydraEnums.Ops.Whirl,
   };

   private PolyHydraEnums.Ops[] Ops =
   {
      // Bank 0
      PolyHydraEnums.Ops.Kis,
      PolyHydraEnums.Ops.Expand,
      PolyHydraEnums.Ops.Chamfer,
      PolyHydraEnums.Ops.Loft,
      PolyHydraEnums.Ops.EdgeMedial,
      PolyHydraEnums.Ops.Lace,
      PolyHydraEnums.Ops.Quinto,
      PolyHydraEnums.Ops.Gyro,

      // Bank 1
      PolyHydraEnums.Ops.Meta,
      PolyHydraEnums.Ops.Truncate,
      PolyHydraEnums.Ops.Zip,
      PolyHydraEnums.Ops.Stake,
      PolyHydraEnums.Ops.Cross,
      PolyHydraEnums.Ops.OppositeLace,
      PolyHydraEnums.Ops.Whirl,
      PolyHydraEnums.Ops.Propeller,

      // New Ops Bank
      // PolyHydraEnums.Ops.JoinKisKis,
      // PolyHydraEnums.Ops.JoinStake,
      // PolyHydraEnums.Ops.Squall,
      // PolyHydraEnums.Ops.JoinSquall,
      // PolyHydraEnums.Ops.Spherize,
      // PolyHydraEnums.Ops.Canonicalize,
      // PolyHydraEnums.Ops.Stretch,
      // PolyHydraEnums.Ops.VertexRotate,




   };
   
   private PolyHydraEnums.Ops[] SecondaryOps =
   {
      PolyHydraEnums.Ops.FaceScale,
//      PolyHydraEnums.Ops.FaceOffset,
      PolyHydraEnums.Ops.Extrude,
      PolyHydraEnums.Ops.Skeleton,

//      PolyHydraEnums.Ops.Shell,

      PolyHydraEnums.Ops.VertexScale,
//      PolyHydraEnums.Ops.VertexRotate,
//      PolyHydraEnums.Ops.VertexFlex,
//      PolyHydraEnums.Ops.FaceRotate,

//      PolyHydraEnums.Ops.FaceKeep,
//      PolyHydraEnums.Ops.FaceRemove,

////      PolyHydraEnums.Ops.FillHoles,
////      PolyHydraEnums.Ops.Hinge,

//      PolyHydraEnums.Ops.AddDual,
//      PolyHydraEnums.Ops.AddMirrorX,
////      PolyHydraEnums.Ops.AddMirrorY,
////      PolyHydraEnums.Ops.AddMirrorZ,
//////      PolyHydraEnums.Ops.Canonicalize,
//      PolyHydraEnums.Ops.Spherize,
//////      PolyHydraEnums.Ops.Recenter,
//////      PolyHydraEnums.Ops.SitLevel,
//      PolyHydraEnums.Ops.Stretch,
//////      PolyHydraEnums.Ops.Weld
   };

   void Start()
   {
      ScanPorts();
      akaiPrefab = GetComponentInChildren<AkaiPrefabController>();
      akaiPrefab.MidiOut = AkaiOutPort;
      novationPrefab = GetComponentInChildren<NovationPrefabController>();
      novationPrefab.MidiOut = NovationOutPort;
      poly.ConwayOperators.Clear();
      InitOps(MAXOPS);
      SetAkaiLEDs();
      SetNovationLEDs();
      FinalisePoly();
      LastSliderValue = new byte[16][];
      for (int i = 0; i < 16; i++)
      {
         LastSliderValue[i] = Enumerable.Repeat((byte)63, 127).ToArray();
      }
   }

   private void InitOps(int count)
   {
      if (poly.ConwayOperators == null)
      {
         poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
      }

      if (poly.ConwayOperators.Count >= count) return;

      for (var i=poly.ConwayOperators.Count; i < count; i++)
      {
         poly.ConwayOperators.Add(new PolyHydra.ConwayOperator
         {
            disabled = true,
            opType = PolyHydraEnums.Ops.Identity,
            amount = 0,
         });
      }

   }

   private PolyHydraEnums.Ops GetOp(int column, int row)
   {
      if (column % 2 == 1)
      {
         int opIndex = row / 2;
         return SecondaryOps[opIndex];
      }
      else
      {
         return Ops[row];
      }
   }

   void SetNovationLEDs()
   {

      if (NovationOutPort == null) return;

      for (int column = 0; column < 8; column++)
      {
         novationPrefab.SetWideButton(column, 0, 3);
         novationPrefab.SetWideButton(column, 1, 5);
      }
   }

   void SetAkaiLEDs()
   {
      if (AkaiOutPort == null) return;
      for (var column = 0; column < MAXOPS; column++)
      {

         if (!poly.ConwayOperators[column].disabled)
         {
            AkaiOutPort.SendNoteOn(0, column+64, 1);
            akaiPrefab.SetColumnButton(column, 2);
         }
         else
         {
            AkaiOutPort.SendNoteOn(0, column+64, 0);
            akaiPrefab.SetColumnButton(column, -1);
         }

         for (var row = 0; row < 8; row++)
         {
            int note = ButtonPosToNote(column, row);
            var op = poly.ConwayOperators[column];
            int nextOpBankNumber = (op.disabled || Array.IndexOf(Ops, op.opType)>=8) ? 0 : 1;


            if (!op.disabled && (op.opType==GetOp(column, row) || (column % 2 == 0 && op.opType==GetOp(column, row + 8))))
            {
               int colorIndex = column % 2 + nextOpBankNumber;
               if (column % 2 == 1)
               {
                  if ((row % 2 == 0) && (op.faceSelections == FaceSelections.Existing || op.faceSelections == FaceSelections.New))
                  {
                     AkaiOutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                     akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                     akaiPrefab.SetGridButtonIcon(column, row, op.opType);

                  }
                  else if ((row % 2 == 1) && (op.faceSelections == FaceSelections.AllNew || op.faceSelections == FaceSelections.NewAlt))
                  {
                     AkaiOutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                     akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                     akaiPrefab.SetGridButtonIcon(column, row, op.opType);
                  }
                  else
                  {
                     AkaiOutPort.SendNoteOn(0, note, 0);
                     akaiPrefab.SetGridButtonLED(column, row, -1);
                     akaiPrefab.SetGridButtonIcon(column, row, PolyHydraEnums.Ops.Identity);

                  }

               }
               else
               {
                  AkaiOutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                  akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                  akaiPrefab.SetGridButtonIcon(column, row, op.opType);
               }

            }
            else
            {
               AkaiOutPort.SendNoteOn(0, note, 0);
               akaiPrefab.SetGridButtonLED(column, row, -1);
               akaiPrefab.SetGridButtonIcon(column, row, PolyHydraEnums.Ops.Identity);
            }
         }
      }
   }

   int ButtonPosToNote(int column, int row)
   {
      return row * 8 + column;
   }

   int[] NoteToButtonPos(int note)
   {
      return new [] {
         note / 8,
         note % 8
      };
   }

   void FinalisePoly()
   {
//      poly.ConwayOperators.Add(new PolyHydra.ConwayOperator()
//      {
//         opType = PolyHydraEnums.Ops.Spherize,
//         amount = 0.25f
//      });
      poly.Rebuild();
   }

   void HandleNoteOn(byte channel, byte note, byte velocity)
   {

      int column;
      int row;

      if (channel==8 && novationPrefab.ButtonIds.Contains(note))
      {
         int buttonIndex = Array.IndexOf(novationPrefab.ButtonIds, note);
         row = buttonIndex / 8;
         column = buttonIndex % 8;
         int newState = novationPrefab.WideButtonStates[buttonIndex];

         if (row == 0)
         {
            var op = poly.ConwayOperators[column];
            if (newState == 3)
            {
               newState = 4;
               op.audioLowAmount = op.animationAmount;
               op.animationAmount = 0;
            }
            else
            {
               newState = 3;
               op.animationAmount = op.audioLowAmount;
               op.audioLowAmount = 0;
            }
            poly.ConwayOperators[column] = op;
         }
         else if (row == 1)
         {
            if (newState == 5)
            {
               newState = 7;
            }
            else
            {
               newState = 5;
            }
//            newState = novationPrefab.WideButtonStates[buttonIndex];
//            newState = (newState + 1) % 8;
         }
         novationPrefab.SetWideButton(column, row, newState);
      }
      else if (channel == 0 && note <= 63)
      {
         // Normal buttons
         var pos = NoteToButtonPos(note);
         row = pos[0];
         column = pos[1];
         var op = poly.ConwayOperators[column];
         var prevOpType = op.opType;

         if (column % 2 == 0)
         {
            // Odd rows - main ops
            int currentRow = Array.IndexOf(Ops, op.opType);
            int bankOffset =  (currentRow < 8 && row==currentRow) ? 8 : 0;  // Toggle
            op.opType = GetOp(column, row + bankOffset);
         }
         else if (column % 2 == 1)
         {
            // Even rows - secondary ops
            op.opType = GetOp(column, row);
            op.faceSelections = ConfigureFaceSelections(column, row);
         }

         op.disabled = false;
         op.amount = UpdateDefault(op.amount, op.opType, prevOpType);
         poly.ConwayOperators[column] = op;

         // Changing a primary op should affect the next active secondary op's selection mode.
         if (column % 2 == 0)
         {
            int nextActiveColumn, nextActiveRow;
            var nextActiveOp = GetNextActiveOp(column, out nextActiveColumn, out nextActiveRow);
            if (nextActiveRow > 0)
            {
               nextActiveOp.faceSelections = ConfigureFaceSelections(nextActiveColumn, nextActiveRow);
               poly.ConwayOperators[nextActiveColumn] = nextActiveOp;
            }
         }

         SetAkaiLEDs();
         if (AkaiInPort != null)
         {
            // Simulate setting the sliders to their last known value
            HandleControlChange(0, Convert.ToByte(column+48), LastSliderValue[channel][column+48]);
         }
         FinalisePoly();
      }
      else if (note >= 64 && note <= 71)
      {
         // Main Column buttons
         column = note - 64;
         var op = poly.ConwayOperators[column];
         var opconfig = PolyHydraEnums.OpConfigs[op.opType];
         op.disabled = !op.disabled;
         op.amount = opconfig.amountDefault;
         poly.ConwayOperators[column] = op;
         SetAkaiLEDs();
         HandleControlChange(0, Convert.ToByte(column+48), LastSliderValue[channel][column+48]);
         FinalisePoly();
      }
      else if (note >= 82 && note <= 89)
      {
         // Main Row Buttons
         row = 7 - (note - 82);
      }
      else if (note==98)
      {
         // Shift Button
      }
   }

   private float UpdateDefault(float opAmount, PolyHydraEnums.Ops opType, PolyHydraEnums.Ops prevOpType)
   {
      // Remaps slider value from previous to new
      return Remap(
         opAmount,
         PolyHydraEnums.OpConfigs[prevOpType].amountSafeMin,
         PolyHydraEnums.OpConfigs[prevOpType].amountSafeMax,
         PolyHydraEnums.OpConfigs[opType].amountSafeMin,
         PolyHydraEnums.OpConfigs[opType].amountSafeMax
      );
   }

   private PolyHydra.ConwayOperator GetPreviousActiveOp(int column)
   {
      PolyHydra.ConwayOperator previousActiveOp = poly.ConwayOperators[column - 1];  // Not sure what to initialize this to
      // Try and find an active op in previous primary columns
      for (int prevActiveColumn = column - 1; prevActiveColumn > 0; prevActiveColumn -= 2)
      {
         previousActiveOp = poly.ConwayOperators[prevActiveColumn];
         if (!previousActiveOp.disabled) break;
      }

      return previousActiveOp;
   }

   private float Remap (float value, float from1, float to1, float from2, float to2) {
      return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
   }

   private PolyHydra.ConwayOperator GetNextActiveOp(int column, out int nextActiveColumn, out int nextActiveRow)
   {
      PolyHydra.ConwayOperator nextActiveOp = poly.ConwayOperators[column + 1];  // Not sure what to initialize this to.
      // Try and find an active op in previous primary columns
      for (nextActiveColumn = column + 1; nextActiveColumn < MAXOPS; nextActiveColumn += 2)
      {
         nextActiveOp = poly.ConwayOperators[nextActiveColumn];
         if (!nextActiveOp.disabled) break;
      }

      if (nextActiveColumn >= MAXOPS)
      {
         nextActiveRow = -1;
         return nextActiveOp;
      }
      nextActiveRow = Array.IndexOf(SecondaryOps, poly.ConwayOperators[nextActiveColumn].opType) * 2;
      return nextActiveOp;
   }

   private FaceSelections ConfigureFaceSelections(int column, int row)
   {
      FaceSelections selectionType1;
      FaceSelections selectionType2;

      int faceSelectionMode = row % 2;
      if (OpsWithExistingFaceMode.ToList().Contains(GetPreviousActiveOp(column).opType))
      {
         selectionType1 = FaceSelections.Existing;
         selectionType2 = FaceSelections.AllNew;
      }
      else
      {
         selectionType1 = FaceSelections.New;
         selectionType2 = FaceSelections.NewAlt;
      }

      return faceSelectionMode == 0 ? selectionType1 : selectionType2;
   }

   void HandleNoteOff(byte channel, byte note)
   {
      Debug.Log($"{note} off");
   }

   private int TotalShapeCount()
   {
      return Polys.Length + Johnsons.Count;
   }

   void HandleControlChange(byte channel, byte number, byte value)
   {
      LastSliderValue[channel][number] = value;
      currentControlValue = value / 127f;
      currentControlValue = Remap(currentControlValue, SliderDeadZone, 1 - SliderDeadZone, 0, 1);
      currentControlValue = currentControlValue < 0 ? 0 : currentControlValue;
      currentControlValue = currentControlValue > 1 ? 1 : currentControlValue;

      if (channel == 0)
      {
         currentControl = number - 48;
         currentControlBank = controlBanks.AkaiSlider;
         akaiPrefab.SetSlider(currentControl, value);
      }
      else if (channel == 8 && number >= 77 && number <= 84)
      {
         currentControl = number - 77;
         currentControlBank = controlBanks.NovationSlider;
         novationPrefab.SetSlider(currentControl, value);
      }
      else if (channel == 8 && number >= 13 && number <= 20)
      {
         currentControl = number - 13;
         currentControlBank = controlBanks.NovationDialSendA;
         novationPrefab.SetDial(2, currentControl, value);
      }
      else if (channel == 8 && number >= 29 && number <= 36)
      {
         currentControl = number - 29;
         currentControlBank = controlBanks.NovationDialSendB;
         novationPrefab.SetDial(1, currentControl, value);
      }
      else if (channel == 8 && number >= 49 && number <= 56)
      {
         currentControl = number - 49;
         currentControlBank = controlBanks.NovationDialPan;
         novationPrefab.SetDial(0, currentControl, value);
      }

      ControlHasChanged = true;
   }

   void RenderControlChange()
   {
      ControlHasChanged = false;

      if (currentControlBank == controlBanks.AkaiSlider)
      {
         if (currentControl == 8)
         {
            ModifyBaseOp();
         }
         else if (currentControl == 7)
         {
            ModifyAPreset();
         }
         else
         {
            if (currentControl >= poly.ConwayOperators.Count) return;
            var op = poly.ConwayOperators[currentControl];
            var opconfig = PolyHydraEnums.OpConfigs[op.opType];
            op.amount = Mathf.Lerp(opconfig.amountSafeMin, opconfig.amountSafeMax, currentControlValue);
            poly.ConwayOperators[currentControl] = op;
            FinalisePoly();
         }
      }
      else if (currentControlBank == controlBanks.NovationSlider)
      {
         if (currentControl == 7)
         {
            ModifyBaseOp();
         }
         else if (currentControl == 6)
         {
            ModifyAPreset();
         }
         else
         {
            if (currentControl >= poly.ConwayOperators.Count) return;
            var op = poly.ConwayOperators[currentControl];
            var opconfig = PolyHydraEnums.OpConfigs[op.opType];
            var currentState = novationPrefab.WideButtonStates[currentControl + 8];
            if (currentState == 5)
            {
               float amount = Mathf.Lerp(opconfig.amountSafeMin, opconfig.amountSafeMax, currentControlValue);
               amount = Mathf.Round(amount * 100) / 100f;
               op.amount = amount;
            }
            else
            {
               float amount = Mathf.Lerp(0.01f, .99f, currentControlValue);
               amount = Mathf.Round(amount * 100) / 100f;
               op.amount2 = amount;
            }
            poly.ConwayOperators[currentControl] = op;
            FinalisePoly();

         }
      }
      else if (currentControlBank == controlBanks.NovationDialPan)
      {
         var op = poly.ConwayOperators[currentControl];
         var currentState = novationPrefab.WideButtonStates[currentControl];
         float animAmount = currentControlValue * 3f - 1.5f;
         animAmount = Mathf.Round(animAmount * 100) / 100f;
         if (Mathf.Abs(animAmount) <= 0.15f) animAmount = 0; // Snap to zero
         if (currentState == 3)
         {
            op.animationAmount = animAmount;
            op.audioLowAmount = 0;
         }
         else
         {
            op.audioLowAmount = animAmount;
            op.animationAmount = 0;
         }
         if (op.animationAmount != 0 && !op.animate)
         {
            // Assume we're starting to animate here so set rate to somethine visible
            // Otherwise it will look like this dial doesn't do anything
            op.animate = true;
            if (op.animationRate < 0.1f) op.animationRate = 0.5f;
         }
         op.animate = true;
         poly.ConwayOperators[currentControl] = op;
      }
      else if (currentControlBank == controlBanks.NovationDialSendB)
      {
         var op = poly.ConwayOperators[currentControl];
         float animationRate = currentControlValue * 5f;
         animationRate = Mathf.Round(animationRate * 100) / 100f;
         op.animationRate = animationRate;
         poly.ConwayOperators[currentControl] = op;
      }
      else if (currentControlBank == controlBanks.NovationDialSendA)
      {
         if (currentControl >= poly.ConwayOperators.Count) return;
         var op = poly.ConwayOperators[currentControl];
         op.opType = (PolyHydraEnums.Ops) (currentControlValue * (Enum.GetNames(typeof(PolyHydraEnums.Ops)).Length - 5));
         op.amount2 = PolyHydraEnums.OpConfigs[op.opType].amount2Default;
         op.disabled = false;
         poly.ConwayOperators[currentControl] = op;
         FinalisePoly();
      }
   }

   private void ModifyAPreset()
   {
            int apresetIndex = Mathf.FloorToInt(currentControlValue * aPresets.Items.Count);
            var apreset = aPresets.Items[apresetIndex];
            aPresets.ApplyPresetToPoly(apreset);
   }

   private void ModifyBaseOp()
   {
            int shapeIndex = Mathf.FloorToInt(currentControlValue * TotalShapeCount());
            if (shapeIndex < Polys.Length)
            {
               poly.ShapeType = PolyHydraEnums.ShapeTypes.Uniform;
               var polyType = Polys[shapeIndex];
               poly.UniformPolyType = polyType;
            }
            else
            {
               poly.ShapeType = PolyHydraEnums.ShapeTypes.Johnson;
               int johnsonIndex = shapeIndex - Polys.Length;
               var johnsonType = Johnsons[johnsonIndex];
               poly.JohnsonPolyType = johnsonType.Item2;
               poly.PrismP = johnsonType.Item1;
            }
            FinalisePoly();
   }

   void ScanPorts()
   {
      _probe = new MidiProbe(MidiProbe.Mode.Out);
      for (var i = 0; i < _probe.PortCount; i++)
      {
         if (_probe.GetPortName(i).Contains("APC MINI"))
         {
            AkaiOutPort = new MidiOutPort(i);
         }

         if (_probe.GetPortName(i).StartsWith("Launch Control XL"))
         {
            NovationOutPort = new MidiOutPort(i);
         }
      }

      _probe = new MidiProbe(MidiProbe.Mode.In);
      for (var i = 0; i < _probe.PortCount; i++)
      {
         if (_probe.GetPortName(i).Contains("APC MINI"))
         {
            AkaiInPort = new MidiInPort(i)
            {
               OnNoteOn = (channel, note, velocity) => HandleNoteOn(channel, note, velocity),
               OnNoteOff = (channel, note) => HandleNoteOff(channel, note),
               OnControlChange = (channel, number, value) => HandleControlChange(channel, number, value)
            };
         }
         if (_probe.GetPortName(i).StartsWith("Launch Control XL"))
         {
            NovationInPort = new MidiInPort(i)
            {
               OnNoteOn = (channel, note, velocity) => HandleNoteOn(channel, note, velocity),
               OnNoteOff = (channel, note) => HandleNoteOff(channel, note),
               OnControlChange = (channel, number, value) => HandleControlChange(channel, number, value)
            };
         }
      }
   }

   void Update()
   {
      // Check if we have enough ops (we might have loaded a preset with < 3 ops)
      if (poly.ConwayOperators.Count < MAXOPS)
      {
         InitOps(MAXOPS);
         SetAkaiLEDs();
      }

      if (AkaiInPort != null)
      {
         AkaiInPort.ProcessMessages();
      }

      if (NovationInPort != null)
      {
         NovationInPort.ProcessMessages();
      }

      if (ControlHasChanged)
      {
         if (Time.frameCount % UpdateSliderEvery != 0) return;
         if (Time.frameCount == LastFrameRendered) return; // We've already rendered on this frame
         LastFrameRendered = Time.frameCount;
         RenderControlChange();
      }
   }

   void DisposePort()
   {
      if (AkaiOutPort!=null) AkaiOutPort.Dispose();
      if (NovationOutPort!=null) NovationOutPort.Dispose();
      if (AkaiInPort!=null) AkaiInPort.Dispose();
      if (NovationInPort!=null) NovationInPort.Dispose();
   }

   void OnDestroy()
   {
      _probe?.Dispose();
      DisposePort();
   }
}
