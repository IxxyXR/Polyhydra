using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Conway;
using RtMidi.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;


public class PolyMidi : MonoBehaviour
{

   public PolyHydra poly;
   public int UpdateEvery = 4;
   private int LastFrameRendered = -1;
   private AppearancePresets aPresets;

   MidiProbe _probe;
   MidiOutPort OutPort;
   MidiInPort InPort;
   int[] Colors = {1, 3, 5};

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
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Prism,
      PolyTypes.Polygonal_Antiprism,
      PolyTypes.Polygonal_Antiprism,
      PolyTypes.Polygonal_Antiprism,
      PolyTypes.Polygonal_Antiprism,
      PolyTypes.Polygonal_Antiprism,
      PolyTypes.Polygonal_Antiprism,
   };

   private PolyHydra.Ops[] OpsWithExistingFaceMode =
   {
      PolyHydra.Ops.Expand,
      PolyHydra.Ops.Loft,
      PolyHydra.Ops.Chamfer,
      PolyHydra.Ops.Quinto,
      PolyHydra.Ops.OppositeLace,
      PolyHydra.Ops.Stake,
   };

   private PolyHydra.Ops[] Ops =
   {
      PolyHydra.Ops.Kis,
//      PolyHydra.Ops.Ambo,
//      PolyHydra.Ops.Zip,
      PolyHydra.Ops.Expand,
//      PolyHydra.Ops.Bevel,
//      PolyHydra.Ops.Join,
//      PolyHydra.Ops.Needle,
//      PolyHydra.Ops.Ortho,
//      PolyHydra.Ops.Meta,
//      PolyHydra.Ops.Truncate,
      PolyHydra.Ops.Gyro,
//      PolyHydra.Ops.Snub,
//      PolyHydra.Ops.Subdivide,
      PolyHydra.Ops.Loft,
      PolyHydra.Ops.Chamfer,
      PolyHydra.Ops.Quinto,
//      PolyHydra.Ops.Lace,
//      PolyHydra.Ops.JoinedLace,
      PolyHydra.Ops.OppositeLace,
      PolyHydra.Ops.Stake,
//      PolyHydra.Ops.Medial,
//      PolyHydra.Ops.EdgeMedial,
      PolyHydra.Ops.Propeller,
//      PolyHydra.Ops.Whirl,
//      PolyHydra.Ops.Volute,
//      PolyHydra.Ops.Exalt,
//      PolyHydra.Ops.Yank,
//      PolyHydra.Ops.Extrude,
//      PolyHydra.Ops.Shell,
//      PolyHydra.Ops.VertexScale,
//      PolyHydra.Ops.VertexRotate,
//      PolyHydra.Ops.VertexFlex,
//      PolyHydra.Ops.FaceOffset,
//      PolyHydra.Ops.FaceScale,
//      PolyHydra.Ops.FaceRotate,
//      PolyHydra.Ops.FaceRemove,
//      PolyHydra.Ops.FaceKeep,
//      PolyHydra.Ops.FillHoles,
//      PolyHydra.Ops.Hinge,
//      PolyHydra.Ops.AddDual,
//      PolyHydra.Ops.AddMirrorX,
//      PolyHydra.Ops.AddMirrorY,
//      PolyHydra.Ops.AddMirrorZ,
//      PolyHydra.Ops.Canonicalize,
//      PolyHydra.Ops.Spherize,
//      PolyHydra.Ops.Recenter,
//      PolyHydra.Ops.SitLevel,
//      PolyHydra.Ops.Stretch,
//      PolyHydra.Ops.Weld
   };
   
   private PolyHydra.Ops[] SecondaryOps =
   {
//      PolyHydra.Ops.Kis,
//      PolyHydra.Ops.Ambo,
//      PolyHydra.Ops.Zip,
//      PolyHydra.Ops.Expand,
//      PolyHydra.Ops.Bevel,
//      PolyHydra.Ops.Join,
//      PolyHydra.Ops.Needle,
//      PolyHydra.Ops.Ortho,
//      PolyHydra.Ops.Meta,
//      PolyHydra.Ops.Truncate,
//      PolyHydra.Ops.Gyro,
//      PolyHydra.Ops.Snub,
//      PolyHydra.Ops.Subdivide,
//      PolyHydra.Ops.Loft,
//      PolyHydra.Ops.Chamfer,
//      PolyHydra.Ops.Quinto,
//      PolyHydra.Ops.Lace,
//      PolyHydra.Ops.JoinedLace,
//      PolyHydra.Ops.OppositeLace,
//      PolyHydra.Ops.Stake,
//      PolyHydra.Ops.Medial,
//      PolyHydra.Ops.EdgeMedial,
//      PolyHydra.Ops.Propeller,
//      PolyHydra.Ops.Whirl,
//      PolyHydra.Ops.Volute,
//      PolyHydra.Ops.Exalt,
//      PolyHydra.Ops.Yank,
      PolyHydra.Ops.Extrude,
//      PolyHydra.Ops.Shell,
//      PolyHydra.Ops.VertexScale,
//      PolyHydra.Ops.VertexRotate,
//      PolyHydra.Ops.VertexFlex,
      PolyHydra.Ops.FaceScale,
      PolyHydra.Ops.VertexScale,
//      PolyHydra.Ops.FaceRotate,
      PolyHydra.Ops.Skeleton,
//      PolyHydra.Ops.FaceKeep,
//      PolyHydra.Ops.FillHoles,
//      PolyHydra.Ops.Hinge,
//      PolyHydra.Ops.AddDual,
//      PolyHydra.Ops.AddMirrorX,
//      PolyHydra.Ops.AddMirrorY,
//      PolyHydra.Ops.AddMirrorZ,
//      PolyHydra.Ops.Canonicalize,
//      PolyHydra.Ops.Spherize,
//      PolyHydra.Ops.Recenter,
//      PolyHydra.Ops.SitLevel,
//      PolyHydra.Ops.Stretch,
//      PolyHydra.Ops.Weld
   };

   void Start()
   {
      aPresets = FindObjectOfType<AppearancePresets>();
      ScanPorts();
      poly.ConwayOperators.Clear();
      for (var i=0; i < 8; i++)
      {
         var opType = Ops[i];
         if (poly.ConwayOperators == null)
         {
            poly.ConwayOperators = new List<PolyHydra.ConwayOperator>();
         }
         poly.ConwayOperators.Add(new PolyHydra.ConwayOperator
         {
            disabled = true,
            opType = opType,
            amount = poly.opconfigs[opType].amountDefault
         });
      }
      SetLEDs();
      FinalisePoly();

//      OutPort.SendAllOff(0);

   }

   private PolyHydra.Ops GetOp(int column, int row)
   {
      if (column % 2 == 1)
      {
         int opIndex = row % 4;
         return SecondaryOps[opIndex];
      }
      else
      {
         return Ops[row];
      }
   }

   void SetLEDs()
   {
      for (var column = 0; column < 8; column++)
      {
         if (!poly.ConwayOperators[column].disabled)
         {
            OutPort.SendNoteOn(0, column+64, 1);
         }
         else
         {
            OutPort.SendNoteOn(0, column+64, 0);
         }
         for (var row = 0; row < 8; row++)
         {
            int note = ButtonPosToNote(column, row);
            var op = poly.ConwayOperators[column];

            if (!op.disabled && op.opType==GetOp(column, row))
            {

               if (column % 2 == 1)
               {
                  if (row <= 3 && (op.faceSelections == ConwayPoly.FaceSelections.Existing ||
                                   op.faceSelections == ConwayPoly.FaceSelections.New))
                  {
                     OutPort.SendNoteOn(0, note, Colors[0]);
                  }
                  else if (row > 3 && (op.faceSelections == ConwayPoly.FaceSelections.AllNew ||
                                       op.faceSelections == ConwayPoly.FaceSelections.NewAlt))
                  {
                     OutPort.SendNoteOn(0, note, Colors[0]);
                  }
                  else
                  {
                     OutPort.SendNoteOn(0, note, 0);
                  }

               }
               else
               {
                  OutPort.SendNoteOn(0, note, Colors[0]);
               }

            }
            else
            {
               OutPort.SendNoteOn(0, note, 0);
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
         var pos = NoteToButtonPos(note);
         row = pos[0];
         column = pos[1];
         Debug.Log($"Column: {column} Row: {row}");
         var op = poly.ConwayOperators[column];

         ConwayPoly.FaceSelections selectionType1 = ConwayPoly.FaceSelections.All;
         ConwayPoly.FaceSelections selectionType2 = ConwayPoly.FaceSelections.All;


         if (column % 2 == 0)
         {
            op.opType = GetOp(column, row);
         }
         else if (column % 2 == 1)
         {
            int faceSelectionMode = -1;
            op.opType = GetOp(column, row);
            faceSelectionMode = row <= 3 ? 0 : 1;
            if (OpsWithExistingFaceMode.ToList().Contains(poly.ConwayOperators[column - 1].opType))
            {
               selectionType1 = ConwayPoly.FaceSelections.Existing;
               selectionType2 = ConwayPoly.FaceSelections.AllNew;
            }
            else
            {
               selectionType1 = ConwayPoly.FaceSelections.New;
               selectionType2 = ConwayPoly.FaceSelections.NewAlt;
            }

            if (faceSelectionMode == 0)
            {
               op.faceSelections = selectionType1;
            }
            else
            {
               op.faceSelections = selectionType2;
            }
         }

         op.disabled = false;
         op.amount = poly.opconfigs[op.opType].amountDefault;
         poly.ConwayOperators[column] = op;
         SetLEDs();
         FinalisePoly();
      }
      else if (note >= 64 && note <= 71)
      {
         column = note - 64;
         Debug.Log($"Main column button: {column}");
         var op = poly.ConwayOperators[column];
         var opconfig = poly.opconfigs[op.opType];
         op.disabled = !op.disabled;
         op.amount = opconfig.amountDefault;
         poly.ConwayOperators[column] = op;
         SetLEDs();
         FinalisePoly();
      }
      else if (note >= 82 && note <= 89)
      {
         row = 7 - (note - 82);
         Debug.Log($"Main row button: {row}");
      }
      else if (note==98)
      {
         Debug.Log($"Shift button");
      }
   }

   void HandleNoteOff(byte channel, byte note)
   {
      //Debug.Log($"{note} off");
   }

   private int TotalShapeCount()
   {
      return Polys.Length;
   }

   void HandleControlChange(byte channel, byte number, byte value)
   {
      if (Time.frameCount % UpdateEvery != 0) return;
      if (Time.frameCount == LastFrameRendered) return; // We've already rendered on this frame
      LastFrameRendered = Time.frameCount;
      int slider = number - 48;
      if (slider == 8)
      {
         int shapeIndex = Mathf.FloorToInt((value / 127f) * TotalShapeCount());
         if (shapeIndex < Polys.Length)
         {
            var polyType = Polys[shapeIndex];
            poly.UniformPolyType = polyType;

            if (polyType == PolyTypes.Polygonal_Prism || polyType == PolyTypes.Polygonal_Antiprism)
            {
               int prismType = shapeIndex - Array.IndexOf(Polys, polyType);
               poly.PrismP = prismType + 3;
            }
         }
         FinalisePoly();
      }
      else if (slider == 7)
      {
         int apresetIndex = Mathf.FloorToInt((value / 127f) * aPresets.Items.Count);
         var apreset = aPresets.Items[apresetIndex];
         aPresets.ApplyPresetToPoly(apreset);
      }
      else
      {
         if (slider >= poly.ConwayOperators.Count) return;
         var op = poly.ConwayOperators[slider];
         var opconfig = poly.opconfigs[op.opType];
         float amount = value / 127f;
         Debug.Log($"Slider: {slider} Op: {op.opType} Amount: {amount}");
         op.amount = Mathf.Lerp(opconfig.amountSafeMin, opconfig.amountSafeMax, amount);
         poly.ConwayOperators[slider] = op;
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
            InPort = new MidiInPort(i)
            {
               OnNoteOn = (channel, note, velocity) => HandleNoteOn(channel, note, velocity),
               OnNoteOff = (channel, note) => HandleNoteOff(channel, note),
               OnControlChange = (channel, number, value) => HandleControlChange(channel, number, value)
            };
            break;
         }
      }
   }

   void Update()
   {
      InPort.ProcessMessages();
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
