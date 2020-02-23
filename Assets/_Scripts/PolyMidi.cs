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
   private MidiOutPort OutPort;
   private MidiInPort AkaiInPort;
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

   private List<(int, PolyHydra.JohnsonPolyTypes)> Johnsons = new List<(int, PolyHydra.JohnsonPolyTypes)>
   {
      (3, PolyHydra.JohnsonPolyTypes.Prism),
      (5, PolyHydra.JohnsonPolyTypes.Prism),
      (6, PolyHydra.JohnsonPolyTypes.Prism),
//      (7, PolyHydra.JohnsonPolyTypes.Prism),
      (8, PolyHydra.JohnsonPolyTypes.Prism),

//      (3, PolyHydra.JohnsonPolyTypes.Antiprism),
      (4, PolyHydra.JohnsonPolyTypes.Antiprism),
      (5, PolyHydra.JohnsonPolyTypes.Antiprism),
      (6, PolyHydra.JohnsonPolyTypes.Antiprism),
//      (7, PolyHydra.JohnsonPolyTypes.Antiprism),
//      (8, PolyHydra.JohnsonPolyTypes.Antiprism),

//      (4, PolyHydra.JohnsonPolyTypes.Pyramid),
//      (5, PolyHydra.JohnsonPolyTypes.Pyramid),
//      (6, PolyHydra.JohnsonPolyTypes.Pyramid),
////      (7, PolyHydra.JohnsonPolyTypes.Pyramid),
//      (8, PolyHydra.JohnsonPolyTypes.Pyramid),

//      (3, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),
//      (4, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),
//      (5, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),
//      (6, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),
//      (7, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),
//      (8, PolyHydra.JohnsonPolyTypes.ElongatedPyramid),

//      (3, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),
//      (4, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),
//      (5, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),
//      (6, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),
//      (7, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),
//      (8, PolyHydra.JohnsonPolyTypes.GyroelongatedPyramid),

      (3, PolyHydra.JohnsonPolyTypes.Dipyramid),
      (5, PolyHydra.JohnsonPolyTypes.Dipyramid),
      (6, PolyHydra.JohnsonPolyTypes.Dipyramid),
//      (7, PolyHydra.JohnsonPolyTypes.Dipyramid),
//      (8, PolyHydra.JohnsonPolyTypes.Dipyramid),

      (3, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),
      (4, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),
      (5, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),
      (6, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),
//      (7, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),
//      (8, PolyHydra.JohnsonPolyTypes.ElongatedDipyramid),

      (3, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),
      (4, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (5, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),
      (6, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (7, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),
//      (8, PolyHydra.JohnsonPolyTypes.GyroelongatedDipyramid),

//      (3, PolyHydra.JohnsonPolyTypes.Cupola),
//      (4, PolyHydra.JohnsonPolyTypes.Cupola),
//      (5, PolyHydra.JohnsonPolyTypes.Cupola),
//      (6, PolyHydra.JohnsonPolyTypes.Cupola),
//      (7, PolyHydra.JohnsonPolyTypes.Cupola),
//      (8, PolyHydra.JohnsonPolyTypes.Cupola),

//      (3, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//      (4, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//      (5, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//      (6, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//      (7, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//      (8, PolyHydra.JohnsonPolyTypes.ElongatedCupola),
//
//      (3, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),
//      (4, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),
//      (5, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),
//      (6, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),
//      (7, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),
//      (8, PolyHydra.JohnsonPolyTypes.GyroelongatedCupola),

      (3, PolyHydra.JohnsonPolyTypes.Bicupola),
      (4, PolyHydra.JohnsonPolyTypes.Bicupola),
//      (5, PolyHydra.JohnsonPolyTypes.Bicupola),
//      (6, PolyHydra.JohnsonPolyTypes.Bicupola),
//      (7, PolyHydra.JohnsonPolyTypes.Bicupola),
//      (8, PolyHydra.JohnsonPolyTypes.Bicupola),

//      (3, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),
//      (4, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),
//      (5, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),
//      (6, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),
//      (7, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),
//      (8, PolyHydra.JohnsonPolyTypes.ElongatedBicupola),

//      (3, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),
//      (4, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),
//      (5, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),
//      (6, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),
//      (7, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),
//      (8, PolyHydra.JohnsonPolyTypes.GyroelongatedBicupola),

      (3, PolyHydra.JohnsonPolyTypes.Rotunda),
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
   private PolyHydra.Ops[] OpsWithExistingFaceMode =
   {
//      PolyHydra.Ops.Kis,
      PolyHydra.Ops.Zip,
      PolyHydra.Ops.Expand,
//      PolyHydra.Ops.Join,
//      PolyHydra.Ops.Needle,
//      PolyHydra.Ops.Meta,
      PolyHydra.Ops.Truncate,
//      PolyHydra.Ops.Gyro,
      PolyHydra.Ops.Loft,
      PolyHydra.Ops.Chamfer,
      PolyHydra.Ops.Quinto,
      PolyHydra.Ops.Lace,
      PolyHydra.Ops.OppositeLace,
      PolyHydra.Ops.Stake,
      PolyHydra.Ops.Propeller,
      PolyHydra.Ops.Whirl,
   };

   private PolyHydra.Ops[] Ops =
   {
      // Bank 0
      PolyHydra.Ops.Kis,
      PolyHydra.Ops.Expand,
      PolyHydra.Ops.Chamfer,
      PolyHydra.Ops.Loft,
      PolyHydra.Ops.EdgeMedial,
      PolyHydra.Ops.Lace,
      PolyHydra.Ops.Quinto,
      PolyHydra.Ops.Gyro,

      // Bank 1
      PolyHydra.Ops.Meta,
      PolyHydra.Ops.Truncate,
      PolyHydra.Ops.Zip,
      PolyHydra.Ops.Stake,
      PolyHydra.Ops.Cross,
      PolyHydra.Ops.OppositeLace,
      PolyHydra.Ops.Whirl,
      PolyHydra.Ops.Propeller,

      // New Ops Bank
      // PolyHydra.Ops.JoinKisKis,
      // PolyHydra.Ops.JoinStake,
      // PolyHydra.Ops.Squall,
      // PolyHydra.Ops.JoinSquall,
      // PolyHydra.Ops.Spherize,
      // PolyHydra.Ops.Canonicalize,
      // PolyHydra.Ops.Stretch,
      // PolyHydra.Ops.VertexRotate,




   };
   
   private PolyHydra.Ops[] SecondaryOps =
   {
      PolyHydra.Ops.FaceScale,
//      PolyHydra.Ops.FaceOffset,
      PolyHydra.Ops.Extrude,
      PolyHydra.Ops.Skeleton,

//      PolyHydra.Ops.Shell,

      PolyHydra.Ops.VertexScale,
//      PolyHydra.Ops.VertexRotate,
//      PolyHydra.Ops.VertexFlex,
//      PolyHydra.Ops.FaceRotate,

//      PolyHydra.Ops.FaceKeep,
//      PolyHydra.Ops.FaceRemove,

////      PolyHydra.Ops.FillHoles,
////      PolyHydra.Ops.Hinge,

//      PolyHydra.Ops.AddDual,
//      PolyHydra.Ops.AddMirrorX,
////      PolyHydra.Ops.AddMirrorY,
////      PolyHydra.Ops.AddMirrorZ,
//////      PolyHydra.Ops.Canonicalize,
//      PolyHydra.Ops.Spherize,
//////      PolyHydra.Ops.Recenter,
//////      PolyHydra.Ops.SitLevel,
//      PolyHydra.Ops.Stretch,
//////      PolyHydra.Ops.Weld
   };

   void Start()
   {
      akaiPrefab = GetComponentInChildren<AkaiPrefabController>();
      novationPrefab = GetComponentInChildren<NovationPrefabController>();
      ScanPorts();
      poly.ConwayOperators.Clear();
      InitOps(MAXOPS);
      SetLEDs();
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
            opType = PolyHydra.Ops.Identity,
            amount = 0,
         });
      }

   }

   private PolyHydra.Ops GetOp(int column, int row)
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

   void SetLEDs()
   {
      for (var column = 0; column < MAXOPS; column++)
      {

         if (!poly.ConwayOperators[column].disabled)
         {
            OutPort.SendNoteOn(0, column+64, 1);
            akaiPrefab.SetColumnButton(column, 2);
         }
         else
         {
            OutPort.SendNoteOn(0, column+64, 0);
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
                  if ((row % 2 == 0) && (op.faceSelections == ConwayPoly.FaceSelections.Existing || op.faceSelections == ConwayPoly.FaceSelections.New))
                  {
                     OutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                     akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                     akaiPrefab.SetGridButtonIcon(column, row, op.opType);

                  }
                  else if ((row % 2 == 1) && (op.faceSelections == ConwayPoly.FaceSelections.AllNew || op.faceSelections == ConwayPoly.FaceSelections.NewAlt))
                  {
                     OutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                     akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                     akaiPrefab.SetGridButtonIcon(column, row, op.opType);
                  }
                  else
                  {
                     OutPort.SendNoteOn(0, note, 0);
                     akaiPrefab.SetGridButtonLED(column, row, -1);
                     akaiPrefab.SetGridButtonIcon(column, row, PolyHydra.Ops.Identity);

                  }

               }
               else
               {
                  OutPort.SendNoteOn(0, note, MidiColorValues[colorIndex]);
                  akaiPrefab.SetGridButtonLED(column, row, colorIndex);
                  akaiPrefab.SetGridButtonIcon(column, row, op.opType);
               }

            }
            else
            {
               OutPort.SendNoteOn(0, note, 0);
               akaiPrefab.SetGridButtonLED(column, row, -1);
               akaiPrefab.SetGridButtonIcon(column, row, PolyHydra.Ops.Identity);
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
//         opType = PolyHydra.Ops.Spherize,
//         amount = 0.25f
//      });
      poly.Rebuild();
   }

   void HandleNoteOn(byte channel, byte note, byte velocity)
   {

      int column;
      int row;

      if (note <= 63)
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

         SetLEDs();
         HandleControlChange(0, Convert.ToByte(column+48), LastSliderValue[channel][column+48]);
         FinalisePoly();
      }
      else if (note >= 64 && note <= 71)
      {

         // Main Column buttons

         column = note - 64;
         var op = poly.ConwayOperators[column];
         var opconfig = poly.opconfigs[op.opType];
         op.disabled = !op.disabled;
         op.amount = opconfig.amountDefault;
         poly.ConwayOperators[column] = op;
         SetLEDs();
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

   private float UpdateDefault(float opAmount, PolyHydra.Ops opType, PolyHydra.Ops prevOpType)
   {
      // Remaps slider value from previous to new
      return Remap(
         opAmount,
         poly.opconfigs[prevOpType].amountSafeMin,
         poly.opconfigs[prevOpType].amountSafeMax,
         poly.opconfigs[opType].amountSafeMin,
         poly.opconfigs[opType].amountSafeMax
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

   private ConwayPoly.FaceSelections ConfigureFaceSelections(int column, int row)
   {
      ConwayPoly.FaceSelections selectionType1;
      ConwayPoly.FaceSelections selectionType2;

      int faceSelectionMode = row % 2;
      if (OpsWithExistingFaceMode.ToList().Contains(GetPreviousActiveOp(column).opType))
      {
         selectionType1 = ConwayPoly.FaceSelections.Existing;
         selectionType2 = ConwayPoly.FaceSelections.AllNew;
      }
      else
      {
         selectionType1 = ConwayPoly.FaceSelections.New;
         selectionType2 = ConwayPoly.FaceSelections.NewAlt;
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
            int shapeIndex = Mathf.FloorToInt(currentControlValue * TotalShapeCount());
            if (shapeIndex < Polys.Length)
            {
               poly.ShapeType = PolyHydra.ShapeTypes.Uniform;
               var polyType = Polys[shapeIndex];
               poly.UniformPolyType = polyType;
            }
            else
            {
               poly.ShapeType = PolyHydra.ShapeTypes.Johnson;
               int johnsonIndex = shapeIndex - Polys.Length;
               var johnsonType = Johnsons[johnsonIndex];
               poly.JohnsonPolyType = johnsonType.Item2;
               poly.PrismP = johnsonType.Item1;
            }
            FinalisePoly();
         }
         else if (currentControl == 7)
         {
            int apresetIndex = Mathf.FloorToInt(currentControlValue * aPresets.Items.Count);
            var apreset = aPresets.Items[apresetIndex];
            aPresets.ApplyPresetToPoly(apreset);
         }
         else
         {
            if (currentControl >= poly.ConwayOperators.Count) return;
            var op = poly.ConwayOperators[currentControl];
            var opconfig = poly.opconfigs[op.opType];
            op.amount = Mathf.Lerp(opconfig.amountSafeMin, opconfig.amountSafeMax, currentControlValue);
            poly.ConwayOperators[currentControl] = op;
            FinalisePoly();
         }
      }
      else if (currentControlBank == controlBanks.NovationSlider)
      {
         if (currentControl >= poly.ConwayOperators.Count) return;
         var op = poly.ConwayOperators[currentControl];
         var opconfig = poly.opconfigs[op.opType];
         op.amount = Mathf.Lerp(opconfig.amountSafeMin, opconfig.amountSafeMax, currentControlValue);
         poly.ConwayOperators[currentControl] = op;
         FinalisePoly();
      }
      else if (currentControlBank == controlBanks.NovationDialPan)
      {
         var op = poly.ConwayOperators[currentControl];
         op.animationAmount = currentControlValue * 3f - 1.5f;
         if (op.animationAmount != 0 && !op.animate)
         {
            // Assume we're starting to animate here so set rate to somethine visible
            // Otherwise it will look like this dial doesn't do anything
            op.animate = true;
            if (op.animationRate < 0.1f) op.animationRate = 0.5f;
         }
         poly.ConwayOperators[currentControl] = op;
      }
      else if (currentControlBank == controlBanks.NovationDialSendB)
      {
         var op = poly.ConwayOperators[currentControl];
         op.animationRate = currentControlValue * 3f;
         poly.ConwayOperators[currentControl] = op;
      }
      else if (currentControlBank == controlBanks.NovationDialSendA)
      {
         if (currentControl >= poly.ConwayOperators.Count) return;
         var op = poly.ConwayOperators[currentControl];
         op.opType = (PolyHydra.Ops) (currentControlValue * Enum.GetNames(typeof(PolyHydra.Ops)).Length - 1);
         op.disabled = false;
         poly.ConwayOperators[currentControl] = op;
         FinalisePoly();
      }
   }

   void ScanPorts()
   {
      _probe = new MidiProbe(MidiProbe.Mode.Out);
      for (var i = 0; i < _probe.PortCount; i++)
      {
         {
            OutPort = new MidiOutPort(i);
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
         SetLEDs();
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
      OutPort.Dispose();
   }

   void OnDestroy()
   {
      _probe?.Dispose();
      DisposePort();
   }
}
