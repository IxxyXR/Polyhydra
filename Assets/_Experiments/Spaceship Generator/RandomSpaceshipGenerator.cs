using Conway;
using UnityEngine;
using Random = UnityEngine.Random;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RandomSpaceshipGenerator : MonoBehaviour
{

    public Material material;
    [Range(0f, 1f)]
    public float ChanceOfSimpleSegment = 0.85f;
    [Range(0f, 1f)]
    public float ChanceOfLaceSegment = 0.75f;
    [Range(0f, 1f)]
    public float ChanceOfTruncateSegment = 0.75f;
    [Range(0f, 1f)]
    public float ChanceOfFins = 0.5f;
    [Range(0f, 1f)]
    public float ChanceOfWings = 0.25f;
    [Range(0f, 1f)]
    public float ChanceOfSharpNose = 0.25f;
    [Range(0f, 1f)]
    public float ChanceOfEngineVariant = 0.5f;




    void Start()
    {
        Generate();
    }

    private void OnValidate()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        int numSides = Random.Range(4, 8);
        var spaceship = JohnsonPoly.Prism(numSides);
        var wings = new ConwayPoly();
        float angleCorrection = 180f / numSides;
        if (numSides % 2 != 0) angleCorrection /= 2f;
        spaceship = spaceship.Rotate(Vector3.up, angleCorrection);
        spaceship = spaceship.Rotate(Vector3.left, -90);

        float loftLow = -.25f;
        float loftHigh = 0.75f;

        void MakeWings()
        {
            if (Random.value < ChanceOfWings)
            {
                var wingFaces = spaceship.Duplicate();
                wingFaces = wingFaces.FaceKeep(FaceSelections.AllNew);
                wingFaces = wingFaces.FaceKeep(FaceSelections.FacingLevel);
                wingFaces = wingFaces.FaceScale(Random.Range(0, 0.5f), FaceSelections.All);
                wingFaces = wingFaces.Loft(Random.Range(0, 1f), Random.Range(.5f, 2f));
                for (int i=0; i<Random.Range(0, 3); i++)
                {
                    wingFaces = wingFaces.Loft(Random.Range(0, 1f), Random.Range(.15f, 1.5f), FaceSelections.Existing);
                    if (Random.value < 0.5f)
                    {
                        wingFaces = wingFaces.FaceSlide(Random.Range(-.5f, .5f), Random.Range(0, 1), FaceSelections.Existing);

                    }
                }
                wings.Append(wingFaces);
            }
        }

        for (int i = 0; i < 2; i++)
        {
            int numSections = Random.Range(2, 5);
            for (int j = 0; j <= numSections; j++)
            {
                if (Random.value < ChanceOfSimpleSegment)
                {
                    spaceship = spaceship.Loft(Random.Range(loftLow, loftHigh), Random.Range(.2f, .5f), FaceSelections.FacingStraightForward);
                    MakeWings();
                }
                else
                {
                    if (Random.value < ChanceOfLaceSegment)
                    {
                        spaceship = spaceship.Lace(Random.Range(loftLow, loftHigh), FaceSelections.FacingStraightForward, "", Random.Range(.2f, .5f));
                        MakeWings();
                    }
                    else if (Random.value < ChanceOfTruncateSegment)
                    {
                        spaceship = spaceship.Truncate(Random.Range(loftLow, loftHigh), FaceSelections.FacingForward);
                        MakeWings();
                    }
                    else
                    {
                        spaceship = RibbedExtrude(spaceship, Random.Range(2, 7));
                    }
                }

                if (Random.value < ChanceOfFins)
                {
                    spaceship = spaceship.Loft(Random.Range(.5f, 0), Random.Range(0.05f, .3f), FaceSelections.AllNew);
                }

                spaceship = spaceship.FaceSlide(Random.Range(-.3f, .3f), 0, FaceSelections.FacingStraightForward);

            }
            spaceship = spaceship.Rotate(Vector3.up, 180);

            loftLow = -0.35f;
            loftHigh = 0.15f;

        }

        // Nose
        if (Random.value < ChanceOfSharpNose)
        {
            spaceship = spaceship.Kis(Random.Range(-.2f, 2f), FaceSelections.FacingStraightForward);
        }

        spaceship = spaceship.Loft(0.1f, 0.025f);

        // Engines
        spaceship = spaceship.Rotate(Vector3.up, 180);
        spaceship = spaceship.Loft(Random.Range(.3f, .4f), Random.Range(-.2f, .2f), FaceSelections.FacingStraightForward);
        if (Random.value < ChanceOfEngineVariant)
        {
            spaceship = spaceship.Stake(Random.Range(.2f, .75f), FaceSelections.Existing);
            spaceship = spaceship.Loft(Random.Range(.1f, .3f), Random.Range(-.3f, -.7f), FaceSelections.AllNew);
        }
        else
        {
            spaceship = spaceship.Loft(Random.Range(.5f, .5f), Random.Range(.2f, .5f), FaceSelections.Existing);
            spaceship = spaceship.Loft(Random.Range(.1f, .3f), Random.Range(-.3f, -.7f), FaceSelections.AllNew);
        }
        spaceship = spaceship.Rotate(Vector3.up, 180);


        //spaceship = spaceship.Kis(1f, FaceSelections.FacingForward);
        wings = wings.Loft(0.1f, 0.025f);
        spaceship.Append(wings);

        var mesh = PolyMeshBuilder.BuildMeshFromConwayPoly(spaceship, false);
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;
    }

    private ConwayPoly RibbedExtrude(ConwayPoly poly, int numRibs)
    {
        float translateForwardsPerRib = Random.Range(0.02f, 0.2f);
        float ribDepth = Random.Range(0.02f, 0.2f);
        for (int i=0; i<numRibs; i++)
        {
            poly = poly.Loft(ribDepth, translateForwardsPerRib * 0.25f, FaceSelections.FacingStraightForward);
            poly = poly.Loft(0, translateForwardsPerRib * 0.5f, FaceSelections.FacingStraightForward);
            poly = poly.Loft(-ribDepth, translateForwardsPerRib * 0.25f, FaceSelections.FacingStraightForward);
            poly = poly.Loft(0, translateForwardsPerRib * 0.25f, FaceSelections.FacingStraightForward);
        }

        return poly;
    }

}
