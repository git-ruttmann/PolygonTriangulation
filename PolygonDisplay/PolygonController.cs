
namespace PolygonDisplay
{
    using PolygonTriangulation;
    using System;
    using System.Collections.Generic;
    using Vertex = System.Numerics.Vector2;

    public interface IPolygonForm
    {
        void RefreshState();
    }

    public class PolygonController
    {
        private readonly IPolygonForm form;

        public PolygonController(IPolygonForm form)
        {
            this.form = form;
            this.ActiveStorageId = 1;

            this.GenerateDataOne();
        }

        public int ActiveStorageId { get; private set; }

        public Polygon Polygon { get; private set; }

        public void ActivateStorage(int id)
        {
            this.ActiveStorageId = id;
            if (id == 1)
            {
                this.Polygon = this.GenerateDataOne();
            }
            else if (id == 2)
            {
                this.Polygon = this.GenerateTouchingError();
            }
            else if (id == 3)
            {
                this.Polygon = this.GenerateValidAt_0_14743980();
            }
            else if (id == 4)
            {
                this.Polygon = this.GenerateErrorAt_0_14741980_late();
            }
        }

        private Polygon GenerateDataOne()
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
                .Close();
        }

        private Polygon GenerateTouchingError()
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
                .AddVertices(9, 12, 15, 14, 8, 3, 0, 1, 2, 4, 4, 5, 6, 7, 13, 11, 10, 4)
                .Close();
        }

        private Polygon GenerateErrorAt_0_14741980()
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

        private Polygon GenerateErrorAt_0_14741980_late()
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

        private Polygon GenerateErrorAt_0_14742980()
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

        private Polygon GenerateValidAt_0_14743980()
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

        private Polygon GenerateErrorAt_0_14744980()
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

        private Polygon GenerateDataTwo()
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
    }
}
