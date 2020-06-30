namespace PolygonDisplay
{
    using PolygonTriangulation;
    using System;
    using Vertex = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;

    /// <summary>
    /// Create sample polygons
    /// </summary>
    public static class PolygonSamples
    {
        /// <summary>
        /// The infamous form number one
        /// </summary>
        /// <returns>the polygon</returns>
        public static Polygon Form1()
        {
            var sortedVertices = new[]
            {
                new Vertex(1, 1), // 0
                new Vertex(1, 3),
                new Vertex(1.5f, 3), // 2
                new Vertex(2, 2),
                new Vertex(2, 4), // 4
                new Vertex(2.5f, 1),
                new Vertex(2.5f, 2), // 6
                new Vertex(2.5f, 3),
                new Vertex(3.5f, 2.5f), // 8
                new Vertex(3.5f, 1),
                new Vertex(4, 1.5f), // 10
                new Vertex(4, 3.5f),
                new Vertex(4, 4), // 12
            };

            var polygon = Polygon.Build(sortedVertices)
                .AddVertices(5, 0, 6, 3, 1, 4, 12, 2, 7, 11, 8, 10, 9)
                .Close();

            return polygon;
        }

        /// <summary>
        /// One polygon is inside the other an both touch at a single point
        /// </summary>
        /// <param name="id">1-6</param>
        /// <returns>the polygon</returns>
        public static Polygon InnerFusionSingle(int id)
        {
            var sortedVerticesUpper = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 3),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var sortedVerticesLower = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(3, 0),  // 4
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 7
                new Vertex(6, 1),
            };

            var builder = id <= 3
                ? Polygon.Build(sortedVerticesUpper).AddVertices(0, 2, 4, 7, 8).ClosePartialPolygon()
                : Polygon.Build(sortedVerticesLower).AddVertices(0, 2, 7, 8, 4).ClosePartialPolygon();

            switch (id)
            {
                case 1: // upper left
                    builder = builder.AddVertices(4, 1, 3);
                    break;
                case 2: // upper center
                    builder = builder.AddVertices(4, 3, 5);
                    break;
                case 3: // upper right
                    builder = builder.AddVertices(4, 5, 6);
                    break;
                case 4: // lower left
                    builder = builder.AddVertices(4, 3, 1);
                    break;
                case 5: // lower center
                    builder = builder.AddVertices(4, 5, 3);
                    break;
                case 6: // lower right
                    builder = builder.AddVertices(4, 6, 5);
                    break;
            }

            return builder.Close(4);
        }

        public static Polygon MultiTouch(int id)
        {
            switch (id)
            {
                case 1:
                    return GenerateDualTouch1();
                case 2:
                    return GenerateDualTouch2();
                case 3:
                    return GenerateTripleTouch();
                case 4:
                    return PointFusionLeftToRight();
                case 5:
                    return PointFusionTopAndBottom();
                case 6:
                    return TripleFusionLeftAndRight();
                default:
                    return null;
            }
        }

        public static Polygon MoreFusionTests(int id)
        {
            switch (id)
            {
                case 1:
                    return InnerPolygonLeftMiddleRight();
                case 2:
                    return SwitchFusionFromInnerToOuter();
                case 3:
                    return ThreeTrianglesInsideEachOther();
                case 4:
                    return UnityError1();
                case 5:
                    return ReclusterUnclosedPolygons();
                case 6:
                    return UnityError4();
                default:
                    return null;
            }
        }

        public static Polygon MoreErrorTests(int id)
        {
            switch (id)
            {
                case 1:
                    return UnityError7();
                case 2:
                    return UnityError2();
                case 3:
                    return UnityError3();
                case 4:
                    return UnityError4();
                case 5:
                    return UnityError5();
                case 6:
                    return UnityError6();
                default:
                    return null;
            }
        }

        public static Polygon UnityError1()
        {
            var vertices = new[]
            {
                new Vertex(0.9657705f, 2.9951100f),
                new Vertex(1.0356860f, 1.1592050f),
                new Vertex(1.0419350f, 0.9951099f),
                new Vertex(1.0669380f, 2.9987030f),
                new Vertex(1.2249190f, 1.0016080f),
                new Vertex(1.2542640f, 2.0040030f),
                new Vertex(1.3087590f, 2.0625700f),
                new Vertex(1.9484140f, 1.3601270f),
                new Vertex(1.9769380f, 2.7806830f),
                new Vertex(2.0340610f, 1.2806830f),
                new Vertex(2.0464590f, 2.7161960f),
                new Vertex(2.6672220f, 1.9611600f),
                new Vertex(2.7567350f, 2.0573620f),
                new Vertex(2.9690640f, 3.0662550f),
                new Vertex(2.9781410f, 2.8279110f),
                new Vertex(3.0452290f, 1.0662550f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(15, 4, 2, 1, 0, 3, 13, 14)
                .ClosePartialPolygon()
                .AddVertices(7, 9, 11, 12, 10, 8, 6, 5)
                .Close();

            return polygon;
        }

        public static Polygon UnityError2()
        {
            var vertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 3),
                new Vertex(3, 0),
                new Vertex(4, 1),
                new Vertex(5, 2),
                new Vertex(6, 2),
                new Vertex(8, 4),
                new Vertex(9, 0),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(0, 1, 2, 7, 8, 3)
                .ClosePartialPolygon()
                .AddVertices(4, 6, 5)
                .Close();

            return polygon;
        }

        public static Polygon UnityError3()
        {
            var vertices = new[]
            {
                new Vertex(-2.6894240f, -1.6565340f),
                new Vertex(-2.6589240f, -1.6194120f),
                new Vertex(-2.2095760f, -1.0725000f),
                new Vertex(-1.9794070f, -0.9460075f),
                new Vertex(-1.9501140f, -1.0369280f),
                new Vertex(-1.8938890f, -0.6882691f),
                new Vertex(-1.5807920f, -2.5727720f),
                new Vertex(-1.5521560f, -2.2721070f),
                new Vertex(-1.4488940f, -0.1466549f),
                new Vertex(-1.4197660f, -0.1112021f),
                new Vertex(-1.0620060f, -1.0609700f),
                new Vertex(-0.6894242f, -3.3094500f),
                new Vertex(-0.6648459f, -3.2795350f),
                new Vertex(-0.5916843f, -1.1199080f),
                new Vertex(-0.4425835f, -1.5826870f),
                new Vertex(-0.4251233f, -0.9332325f),
                new Vertex(-0.3044788f, -2.4284580f),
                new Vertex(-0.2893631f, -2.8225260f),
                new Vertex(-0.1644333f, -2.4460070f),
                new Vertex(0.1718097f, -2.2612210f),
                new Vertex(0.5542740f, -1.7957150f),
                new Vertex(0.5802340f, -1.7641180f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(1, 2, 5, 8, 9, 15, 21, 20, 19, 17, 12, 11, 6, 0)
                .ClosePartialPolygon()
                .AddVertices(13, 10, 3, 4, 7, 16, 18, 14)
                .ClosePartialPolygon()
                .Close();

            return polygon;
        }

        public static Polygon UnityError4()
        {
            var vertices = new[]
            {
                new Vertex(0.5204509f, 1.1067680f),
                new Vertex(0.6062286f, 1.3088240f),
                new Vertex(0.6763443f, 1.4739880f),
                new Vertex(0.8025235f, 1.3167940f),
                new Vertex(0.8901310f, 1.2076520f),
                new Vertex(1.1040800f, 2.4815560f),
                new Vertex(1.2380720f, 2.7971850f),
                new Vertex(1.2653450f, 0.7483246f),
                new Vertex(1.3066230f, 2.9586610f),
                new Vertex(1.8197220f, 2.4329830f),
                new Vertex(1.8281890f, 1.1439830f),
                new Vertex(2.5204510f, 0.1443675f),
                new Vertex(2.5346890f, 0.1779069f),
                new Vertex(2.6145320f, 2.3292950f),
                new Vertex(2.9135360f, 1.0703150f),
                new Vertex(3.2722740f, 1.9153500f),
                new Vertex(3.3066230f, 1.9962610f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(10, 14, 12, 11, 7, 0, 1, 2, 3, 4)
                .ClosePartialPolygon()
                .AddVertices(15, 14, 9, 5, 6, 8, 13, 16)
                .ClosePartialPolygon()
                .Close(14);

            return polygon;
        }

        public static Polygon UnityError5()
        {
            var vertices = new[]
            {
                new Vertex(0.5204509f, 1.1067680f),
                new Vertex(0.6062286f, 1.3088240f),
                new Vertex(0.6763443f, 1.4739880f),
                new Vertex(0.8025235f, 1.3167940f),
                new Vertex(0.8901310f, 1.2076520f),
                new Vertex(1.1040800f, 2.4815560f),
                new Vertex(1.2380720f, 2.7971850f),
                new Vertex(1.2653450f, 0.7483246f),
                new Vertex(1.3066230f, 2.9586610f),
                new Vertex(1.8197220f, 2.4329830f),
                new Vertex(1.8281890f, 1.1439830f),
                new Vertex(2.5204510f, 0.1443675f),
                new Vertex(2.5346890f, 0.1779069f),
                new Vertex(2.6145320f, 2.3292950f),
                new Vertex(2.9135360f, 1.0703150f),
                new Vertex(3.2722740f, 1.9153500f),
                new Vertex(3.3066230f, 1.9962610f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(10, 14, 12, 11, 7, 0, 1, 2, 3, 4)
                .ClosePartialPolygon()
                .AddVertices(15, 14, 9, 5, 6, 8, 13, 16)
                .ClosePartialPolygon()
                .Close(14);

            return polygon;
        }

        public static Polygon UnityError6()
        {
            var vertices = new[]
            {
                new Vertex(0.97032920f, 0.81851240f),
                new Vertex(0.97592230f, 0.92531130f),
                new Vertex(1.01729100f, 1.71523700f),
                new Vertex(1.02747200f, 1.90963600f),
                new Vertex(1.07189700f, 2.75792400f),
                new Vertex(1.07534400f, 2.82374700f),
                new Vertex(1.26485600f, 1.71643700f),
                new Vertex(1.38595200f, 0.79852870f),
                new Vertex(2.90666200f, 2.73569500f),
                new Vertex(2.97032900f, 0.72234950f),
                new Vertex(2.97468800f, 0.80557190f),
                new Vertex(3.02283700f, 1.72496800f),
                new Vertex(3.07318800f, 2.68641600f),
                new Vertex(3.07534400f, 2.72758500f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(6, 11, 10, 9, 7, 0, 1, 2)
                .ClosePartialPolygon()
                .AddVertices(12, 11, 3, 4, 5, 8, 13)
                .ClosePartialPolygon()
                .Close(11);

            var builder = PlanePolygonBuilder.CreatePolygonBuilder();
            builder.AddEdge(new Vector3(1.38595200f, 0.79852870f, 0), new Vector3(0.97032920f, 0.81851240f, 0));
            builder.AddEdge(new Vector3(2.97032900f, 0.72234950f, 0), new Vector3(1.38595200f, 0.79852870f, 0));
            builder.AddEdge(new Vector3(3.02283700f, 1.72496800f, 0), new Vector3(2.97468800f, 0.80557190f, 0));
            builder.AddEdge(new Vector3(3.07318800f, 2.68641600f, 0), new Vector3(3.02283700f, 1.72496800f, 0));
            builder.AddEdge(new Vector3(3.07534400f, 2.72758500f, 0), new Vector3(3.07318800f, 2.68641600f, 0));
            builder.AddEdge(new Vector3(2.97468800f, 0.80557190f, 0), new Vector3(2.97032900f, 0.72234950f, 0));
            builder.AddEdge(new Vector3(2.90666200f, 2.73569500f, 0), new Vector3(3.07534400f, 2.72758500f, 0));
            builder.AddEdge(new Vector3(1.07534400f, 2.82374700f, 0), new Vector3(2.90666200f, 2.73569500f, 0));
            builder.AddEdge(new Vector3(1.02747200f, 1.90963600f, 0), new Vector3(1.07189700f, 2.75792400f, 0));
            builder.AddEdge(new Vector3(0.97592230f, 0.92531120f, 0), new Vector3(1.01729100f, 1.71523700f, 0));
            builder.AddEdge(new Vector3(0.97032920f, 0.81851240f, 0), new Vector3(0.97592230f, 0.92531130f, 0));
            builder.AddEdge(new Vector3(1.07189700f, 2.75792400f, 0), new Vector3(1.07534400f, 2.82374700f, 0));
            builder.AddEdge(new Vector3(3.02283700f, 1.72496800f, 0), new Vector3(1.02747200f, 1.90963600f, 0));
            builder.AddEdge(new Vector3(1.26485600f, 1.71643700f, 0), new Vector3(3.02283700f, 1.72496800f, 0));
            builder.AddEdge(new Vector3(1.01729100f, 1.71523700f, 0), new Vector3(1.26485600f, 1.71643700f, 0));
            var result = builder.BuildPolygon();

            return result.Polygon;
        }

        public static Polygon UnityError7()
        {
            var vertices = new[]
            {
                new Vertex(-2.04168500f, 0.05391948f),
                new Vertex(-1.73525800f, -0.39220070f),
                new Vertex(-1.59090200f, 0.81340110f),
                new Vertex(-1.28913800f, -1.04169800f),
                new Vertex(-1.18307500f, 1.50051100f),
                new Vertex(-1.16461800f, -0.62563610f),
                new Vertex(-1.13713300f, 0.22282370f),
                new Vertex(-1.10918100f, 1.08569700f),
                new Vertex(-0.98826190f, -1.09449200f),
                new Vertex(-0.96441320f, 1.46214300f),
                new Vertex(-0.96191410f, 1.11436200f),
                new Vertex(-0.94439380f, -0.58277110f),
                new Vertex(-0.38140370f, -1.20097700f),
                new Vertex(-0.24038320f, -1.22572200f),
                new Vertex(-0.07887594f, -0.41430300f),
                new Vertex(-0.02343902f, 1.29703000f),
                new Vertex(0.27925600f, -0.35023050f),
                new Vertex(0.44308910f, 1.21516900f),
                new Vertex(0.85362760f, 0.61747490f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(11, 14, 15, 17, 18, 16, 13, 12, 8, 3, 1, 0, 2, 4, 9, 15, 10, 7, 6, 5)
                .ClosePartialPolygon()
                .Close(15);

            return polygon;
        }

        public static Polygon ReclusterUnclosedPolygons()
        {
            var vertices = new[]
            {
                new Vertex(1.0162740f, 1.0038640f),
                new Vertex(1.0201770f, 2.9980340f),
                new Vertex(1.0201890f, 3.0038640f),
                new Vertex(1.2682340f, 2.0033770f),
                new Vertex(1.2706930f, 2.0009080f),
                new Vertex(2.0167730f, 1.2519160f),
                new Vertex(2.0184410f, 1.2535790f),
                new Vertex(2.0191380f, 2.7513470f),
                new Vertex(2.0197090f, 2.7519160f),
                new Vertex(2.2345460f, 3.0014980f),
                new Vertex(2.2345840f, 3.0014980f),
                new Vertex(2.3070080f, 2.8231450f),
                new Vertex(2.3070400f, 2.8231530f),
                new Vertex(2.5538380f, 2.2157020f),
                new Vertex(2.6778460f, 1.9104070f),
                new Vertex(2.6778650f, 1.9104250f),
                new Vertex(2.9670200f, 1.1987120f),
                new Vertex(2.9670540f, 1.1985730f),
                new Vertex(3.0161410f, 0.9999681f),
                new Vertex(3.0162930f, 0.9999678f),
                new Vertex(3.0162930f, 1.0001180f),
                new Vertex(3.0164440f, 1.0770620f),
            };

            var polygon = Polygon.Build(vertices)
                .AddVertices(13, 8, 7, 3, 4, 5, 6, 15, 16, 21, 20, 19, 18, 0, 1, 2, 9, 11)
                .ClosePartialPolygon()
                .Close();

            return polygon;
        }

        public static Polygon InnerPolygonLeftMiddleRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(1, 3),  // 2
                new Vertex(2, 2),
                new Vertex(2.5f, 2),
                new Vertex(3, 3),  // 5
                new Vertex(3.5f, 2),
                new Vertex(4, 2),
                new Vertex(5, 2),
                new Vertex(5, 3),  // 9
                new Vertex(6, 1),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 5, 9, 10)
                .ClosePartialPolygon()
                .AddVertices(5, 1, 3)
                .ClosePartialPolygon()
                .AddVertices(5, 4, 6)
                .ClosePartialPolygon()
                .AddVertices(5, 7, 8)
                .Close(5);
        }

        public static Polygon SwitchFusionFromInnerToOuter()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 2),
                new Vertex(1, 0),
                new Vertex(2, 4), // 2
                new Vertex(3, 2),
                new Vertex(5, 1), // 4
                new Vertex(5, 4),
                new Vertex(6, 0), // 6
                new Vertex(6, 3),
                new Vertex(8, 4),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(1, 3, 0, 2, 3, 5, 8, 6)
                .ClosePartialPolygon()
                .AddVertices(3, 4, 7)
                .Close(3);
        }

        public static Polygon ThreeTrianglesInsideEachOther()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(2, 4),
                new Vertex(3, 1),
                new Vertex(3, 3),
                new Vertex(3, 6),
                new Vertex(4, 4),
                new Vertex(5, 5),
                new Vertex(6, 2),
                new Vertex(6, 6),
                new Vertex(7, 1),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(0, 1, 5, 3, 1, 4, 8, 9)
                .ClosePartialPolygon()
                .AddVertices(1, 2, 7, 6)
                .Close(1);
        }

        public static Polygon PointFusionLeftToRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(0, 2),
                new Vertex(1, 1),
                new Vertex(2, 0),
                new Vertex(2, 2),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(2, 0, 1)
                .ClosePartialPolygon()
                .AddVertices(2, 4, 3)
                .Close(2);
        }

        public static Polygon PointFusionTopAndBottom()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(0, 2),
                new Vertex(1, 1),
                new Vertex(2, 0),
                new Vertex(2, 2),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(2, 1, 4)
                .ClosePartialPolygon()
                .AddVertices(2, 3, 0)
                .Close(2);
        }
        
        public static Polygon TripleFusionLeftAndRight()
        {
            var sortedVertices = new[]
            {
                new Vertex(0, 2),
                new Vertex(1, 0),
                new Vertex(1, 1),
                new Vertex(1, 2),
                new Vertex(1, 3),
                new Vertex(1, 4),
                new Vertex(1, 5),
                new Vertex(2, 3),
            };

            return Polygon.Build(sortedVertices)
                .AddVertices(0, 2, 7, 1)
                .ClosePartialPolygon()
                .AddVertices(0, 4, 7, 3)
                .ClosePartialPolygon()
                .AddVertices(0, 6, 7, 5)
                .Close(0, 7);
        }

        public static Polygon GenerateDataOne()
        {
            var vertices = new[]
            {
                new Vertex(-1.009218f, 1.241688f),
                new Vertex(-0.8848248f, 1.403969f),
                new Vertex(-0.6169144f, 1.753481f),
                new Vertex(-0.2567905f, 0.265681f),
                new Vertex(-0.1457446f, 1.866717f),
                new Vertex(-0.09439461f, 0.9056299f),
                new Vertex(-0.07060831f, 0.4604469f),
                new Vertex(0.4499452f, 0.4153259f),
                new Vertex(0.5326312f, -0.7583123f),
                new Vertex(0.7791165f, 2.088959f),
                new Vertex(0.9355021f, 1.773004f),
                new Vertex(0.951596f, 1.471793f),
                new Vertex(0.9893153f, 2.139471f),
                new Vertex(1.01064f, 0.3667254f),
                new Vertex(1.363283f, 0.3253429f),
                new Vertex(1.874112f, 0.9917631f),
            };

            return Polygon.Build(vertices)
                .AddVertices(12, 15, 14, 8, 3, 0, 1, 2, 4, 9)
                .ClosePartialPolygon()
                .AddVertices(7, 13, 11, 10, 4, 5, 6)
                .Close(4);
        }

        public static Polygon GenerateDataTwo()
        {
            var vertices = new[]
            {
                new Vertex(-1.01359200f, 1.25988900f),
                new Vertex(-0.90201720f, 1.40544800f),
                new Vertex(-0.66171440f, 1.71894300f),
                new Vertex(-0.24898220f, 0.26808070f),
                new Vertex(-0.23913420f, 1.82049300f),
                new Vertex(-0.14784980f, 1.84242900f),
                new Vertex(-0.09805020f, 0.91038390f),
                new Vertex(-0.07498235f, 0.47864870f),
                new Vertex(-0.01848418f, 1.87351700f),
                new Vertex(0.45914020f, 0.43235150f),
                new Vertex(0.52825720f, -0.74011080f),
                new Vertex(0.79377420f, 2.06870900f),
                new Vertex(0.93112810f, 1.79120500f),
                new Vertex(0.94526340f, 1.52665100f),
                new Vertex(1.00626600f, 0.38492710f),
                new Vertex(1.01292600f, 2.12137300f),
                new Vertex(1.34609000f, 0.32682190f),
                new Vertex(1.86973800f, 1.00996500f)
            };

            return Polygon.Build(vertices)
                .AddVertices(9, 14, 13, 12, 8, 11, 15, 17, 16, 10, 3, 0, 1, 2, 4, 5, 6, 7)
                .Close();
        }

        public static Polygon GenerateDualTouch1()
        {
            var vertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 0),
                new Vertex(2, 4),
                new Vertex(3, 1),
                new Vertex(4, 0),
            };

            return Polygon.Build(vertices)
                .AddVertices(0, 1, 3, 5, 2)
                .ClosePartialPolygon()
                .AddVertices(1, 2, 4)
                .Close(2, 1);
        }

        public static Polygon GenerateDualTouch2()
        {
            var vertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 0),
                new Vertex(2, 4),
                new Vertex(3, 1),
                new Vertex(4, 0),
            };

            return Polygon.Build(vertices)
                .AddVertices(0, 1, 3, 5, 2)
                .ClosePartialPolygon()
                .AddVertices(1, 2, 4)
                .Close(1, 2);
        }

        public static Polygon GenerateTripleTouch()
        {
            var vertices = new[]
            {
                new Vertex(0, 0),
                new Vertex(1, 2),
                new Vertex(2, 0),
                new Vertex(2, 4),
                new Vertex(3, 2),
                new Vertex(4, 0),
            };

            return Polygon.Build(vertices)
                .AddVertices(0, 1, 3, 4, 5, 2)
                .ClosePartialPolygon()
                .AddVertices(1, 2, 4)
                .Close(1, 2, 4);
        }

        public static Polygon UnityErrors(int id)
        {
            switch (id)
            {
                case 1:
                    return GenerateErrorAt_0_14741980();
                case 2:
                    return GenerateErrorAt_0_14741980_late();
                case 3:
                    return GenerateErrorAt_0_14742980();
                case 4:
                    return GenerateValidAt_0_14743980();
                case 5:
                    return GenerateErrorAt_0_14744980();
                case 6:
                    return GenerateTouchingError();
                default:
                    return null;
            }
        }

        private static Polygon GenerateTouchingError()
        {
            var vertices = new[]
            {
                new Vertex(-1.00921600f, 1.24168100f),
                new Vertex(-0.88481820f, 1.40396900f),
                new Vertex(-0.61689750f, 1.75349400f),
                new Vertex(-0.25679350f, 0.26568000f),
                new Vertex(-0.14574460f, 1.86671700f),
                new Vertex(-0.09439330f, 0.90562810f),
                new Vertex(-0.07060667f, 0.46044010f),
                new Vertex(0.44994170f, 0.41531950f),
                new Vertex(0.53263280f, -0.75831920f),
                new Vertex(0.77911100f, 2.08896600f),
                new Vertex(0.93550380f, 1.77299700f),
                new Vertex(0.95159840f, 1.47177200f),
                new Vertex(0.98930650f, 2.13947800f),
                new Vertex(1.01064200f, 0.36671840f),
                new Vertex(1.36328900f, 0.32534240f),
                new Vertex(1.87411300f, 0.99175620f)
            };

            return Polygon.Build(vertices)
                .AddVertices(9, 12, 15, 14, 8, 3, 0, 1, 2, 4, 5, 6, 7, 13, 11, 10, 4)
                .Close(4);
        }

        private static Polygon GenerateErrorAt_0_14741980()
        {
            var vertices = new[]
            {
                new Vertex(-0.30465070f, 0.45236350f),
                new Vertex(0.16610780f, 0.56549070f),
                new Vertex(0.16683170f, 0.56590190f),
                new Vertex(0.16682580f, 0.56601040f),
                new Vertex(0.16684420f, 0.56566770f),
                new Vertex(0.16788820f, 0.56591840f),
                new Vertex(0.34376790f, 1.29828100f),
                new Vertex(0.64483490f, 1.69104800f),
                new Vertex(1.09183400f, 0.78795030f),
                new Vertex(1.30210200f, 0.83847910f)
            };

            return Polygon.Build(vertices)
                .AddVertices(2, 4, 1, 0, 6, 7, 9, 8, 5, 3)
                .Close();
        }

        private static Polygon GenerateErrorAt_0_14741980_late()
        {
            var vertices = new[]
            {
                new Vertex(-0.30465070f, 0.45236350f),
                new Vertex(0.16610780f, 0.56549070f),
                new Vertex(0.16682580f, 0.56601040f),
                new Vertex(0.16683170f, 0.56590190f),
                new Vertex(0.16684420f, 0.56566770f),
                new Vertex(0.16788760f, 0.56591840f),
                new Vertex(0.34376790f, 1.29828100f),
                new Vertex(0.64483490f, 1.69104800f),
                new Vertex(1.09183400f, 0.78795030f),
                new Vertex(1.30210200f, 0.83847910f)
            };

            return Polygon.Build(vertices)
                .AddVertices(3, 4, 1, 0, 6, 7, 9, 8, 5, 2)
                .Close();
        }

        private static Polygon GenerateErrorAt_0_14742980()
        {
            var vertices = new[]
            {
                new Vertex(-0.30473080f, 0.45230180f),
                new Vertex(0.16594100f, 0.56540820f),
                new Vertex(0.16682510f, 0.56591040f),
                new Vertex(0.16681800f, 0.56604300f),
                new Vertex(0.16684040f, 0.56562420f),
                new Vertex(0.16811590f, 0.56593060f),
                new Vertex(0.34373700f, 1.29828400f),
                new Vertex(0.64482710f, 1.69108100f),
                new Vertex(1.09186000f, 0.78791400f),
                new Vertex(1.30214400f, 0.83844660f)
            };

            return Polygon.Build(vertices)
                .AddVertices(2, 4, 1, 0, 6, 7, 9, 8, 5, 3)
                .Close();
        }

        private static Polygon GenerateValidAt_0_14743980()
        {
            var vertices = new[]
            {
                new Vertex(-0.30481090f, 0.45224000f),
                new Vertex(0.16577410f, 0.56532550f),
                new Vertex(0.16681850f, 0.56591890f),
                new Vertex(0.16681020f, 0.56607560f),
                new Vertex(0.16683670f, 0.56558080f),
                new Vertex(0.16834310f, 0.56594280f),
                new Vertex(0.34370640f, 1.29828600f),
                new Vertex(0.64481930f, 1.69111300f),
                new Vertex(1.09188600f, 0.78787790f),
                new Vertex(1.30218600f, 0.83841430f)
            };

            return Polygon.Build(vertices)
                .AddVertices(2, 4, 1, 0, 6, 7, 9, 8, 5, 3)
                .Close();
        }

        private static Polygon GenerateErrorAt_0_14744980()
        {
            var vertices = new[]
            {
                new Vertex(-0.30489100f, 0.45217830f),
                new Vertex(0.16560720f, 0.56524280f),
                new Vertex(0.16681200f, 0.56592750f),
                new Vertex(0.16680230f, 0.56610810f),
                new Vertex(0.16683280f, 0.56553750f),
                new Vertex(0.16857040f, 0.56595490f),
                new Vertex(0.34367570f, 1.29828900f),
                new Vertex(0.64481150f, 1.69114600f),
                new Vertex(1.09191300f, 0.78784160f),
                new Vertex(1.30222800f, 0.83838190f)
            };

            return Polygon.Build(vertices)
                .AddVertices(2, 4, 1, 0, 6, 7, 9, 8, 5, 3)
                .Close();
        }
    }
}
