using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;
using System.Numerics;

namespace MystIVAssetExplorer.Skybox;

partial class SkyboxControl
{
    private sealed class SkyboxDrawOperation(SkyboxControl owner, ILease<SkyboxModel> boxModelLease) : ICustomDrawOperation
    {
        public Rect Bounds => new(owner.Bounds.Size);

        public void Dispose() => boxModelLease.Dispose();

        public bool Equals(ICustomDrawOperation? other) => other == this;

        public bool HitTest(Point p) => false;

        public void Render(ImmediateDrawingContext context)
        {
            using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!.Lease();
            var canvas = lease.SkCanvas;

            canvas.ClipRect(Bounds.ToSKRect());
            canvas.Translate((float)Bounds.Width / 2, (float)Bounds.Height / 2);

            var scale = (float)(owner.zoom * double.Min(Bounds.Width, Bounds.Height) / 2);
            DrawSkybox(canvas, boxModelLease.LeasedInstance, (float)owner.AngleX, (float)owner.AngleY, scale);
        }

        private static void DrawSkybox(SKCanvas canvas, SkyboxModel boxModel, float angleX, float angleY, float scale)
        {
            var cubeVertices = new Vector3[]
            {
                new(-1,  1,  1), // 0: left-top-front
                new( 1,  1,  1), // 1: right-top-front
                new(-1, -1,  1), // 2: left-bottom-front
                new( 1, -1,  1), // 3: right-bottom-front
                new(-1,  1, -1), // 4: left-top-back
                new( 1,  1, -1), // 5: right-top-back
                new(-1, -1, -1), // 6: left-bottom-back
                new( 1, -1, -1), // 7: right-bottom-back
            };

            var rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, angleY)
                * Quaternion.CreateFromAxisAngle(Vector3.UnitY, angleX);

            var rotatedVertices = new Vector3[cubeVertices.Length];
            for (var i = 0; i < cubeVertices.Length; i++)
                rotatedVertices[i] = Vector3.Transform(cubeVertices[i], rotation);

            var faces = new (SKBitmap? Texture, int[] Vertices)[]
            {
                (boxModel.Front, [0, 1, 2, 3]),
                (boxModel.Back, [5, 4, 7, 6]),
                (boxModel.Left, [4, 0, 6, 2]),
                (boxModel.Right, [1, 5, 3, 7]),
                (boxModel.Top, [4, 5, 0, 1]),
                (boxModel.Bottom, [2, 3, 6, 7]),
            };

            using var paint = new SKPaint
            {
                IsAntialias = false,
                FilterQuality = SKFilterQuality.High,
            };

            foreach (var face in faces)
            {
                if (face.Texture is null) continue;

                var firstPointNotBehindCamera = Array.FindIndex(face.Vertices, i => rotatedVertices[i].Z > 0);
                if (firstPointNotBehindCamera == -1)
                    continue;

                var pts = new SKPoint[4];
                for (int i = 0; i < 4; i++)
                {
                    var v = rotatedVertices[face.Vertices[i]];
                    pts[i] = new SKPoint(v.X / v.Z * scale, v.Y / v.Z * -scale);
                }

                canvas.Save();

                // The point used as the matrix's TransX+Y must have Z > 0.
                if (firstPointNotBehindCamera == 0)
                {
                    canvas.SetMatrix(canvas.TotalMatrix.PreConcat(MapUnitSquareToGivenPoints(pts[0], pts[1], pts[2], pts[3])));
                    canvas.DrawBitmap(face.Texture, new SKRect(0, 0, 1, 1), paint);
                }
                else if (firstPointNotBehindCamera == 1)
                {
                    canvas.SetMatrix(canvas.TotalMatrix.PreConcat(MapNegativeXSquareToGivenPoints(pts[0], pts[1], pts[2], pts[3])));
                    canvas.DrawBitmap(face.Texture, new SKRect(-1, 0, 0, 1), paint);
                }
                else if (firstPointNotBehindCamera == 2)
                {
                    canvas.SetMatrix(canvas.TotalMatrix.PreConcat(MapNegativeYSquareToGivenPoints(pts[0], pts[1], pts[2], pts[3])));
                    canvas.DrawBitmap(face.Texture, new SKRect(0, -1, 1, 0), paint);
                }
                else
                {
                    canvas.SetMatrix(canvas.TotalMatrix.PreConcat(MapNegativeXYSquareToGivenPoints(pts[0], pts[1], pts[2], pts[3])));
                    canvas.DrawBitmap(face.Texture, new SKRect(-1, -1, 0, 0), paint);
                }

                canvas.Restore();
            }
        }

        private static SKMatrix MapUnitSquareToGivenPoints(SKPoint topLeft, SKPoint topRight, SKPoint bottomLeft, SKPoint bottomRight)
        {
            var rightDiff = bottomRight - topRight;
            var bottomDiff = bottomRight - bottomLeft;

            var determinant = rightDiff.X * bottomDiff.Y - bottomDiff.X * rightDiff.Y;

            var topDiff = topRight - topLeft;
            var leftDiff = bottomLeft - topLeft;

            var persp0 = (topDiff.X * bottomDiff.Y - topDiff.Y * bottomDiff.X) / determinant;
            var persp1 = (rightDiff.X * leftDiff.Y - rightDiff.Y * leftDiff.X) / determinant;

            return new SKMatrix
            {
                ScaleX = persp0 * topRight.X + topDiff.X,
                SkewX = persp1 * bottomLeft.X + leftDiff.X,
                TransX = topLeft.X,
                SkewY = persp0 * topRight.Y + topDiff.Y,
                ScaleY = persp1 * bottomLeft.Y + leftDiff.Y,
                TransY = topLeft.Y,
                Persp0 = persp0,
                Persp1 = persp1,
                Persp2 = 1,
            };
        }

        private static SKMatrix MapNegativeXSquareToGivenPoints(SKPoint topLeft, SKPoint topRight, SKPoint bottomLeft, SKPoint bottomRight)
        {
            var leftDiff = bottomLeft - topLeft;
            var bottomDiff = bottomLeft - bottomRight;

            var determinant = leftDiff.X * bottomDiff.Y - bottomDiff.X * leftDiff.Y;

            var topDiff = topLeft - topRight;
            var rightDiff = bottomRight - topRight;

            var persp0 = (bottomDiff.X * topDiff.Y - bottomDiff.Y * topDiff.X) / determinant;
            var persp1 = (leftDiff.X * rightDiff.Y - leftDiff.Y * rightDiff.X) / determinant;

            return new SKMatrix
            {
                ScaleX = persp0 * topLeft.X - topDiff.X,
                SkewX = persp1 * bottomRight.X + rightDiff.X,
                TransX = topRight.X,
                SkewY = persp0 * topLeft.Y - topDiff.Y,
                ScaleY = persp1 * bottomRight.Y + rightDiff.Y,
                TransY = topRight.Y,
                Persp0 = persp0,
                Persp1 = persp1,
                Persp2 = 1,
            };
        }

        private static SKMatrix MapNegativeYSquareToGivenPoints(SKPoint topLeft, SKPoint topRight, SKPoint bottomLeft, SKPoint bottomRight)
        {
            var rightDiff = topRight - bottomRight;
            var topDiff = topRight - topLeft;

            var determinant = rightDiff.X * topDiff.Y - topDiff.X * rightDiff.Y;

            var bottomDiff = bottomRight - bottomLeft;
            var leftDiff = topLeft - bottomLeft;

            var persp0 = (bottomDiff.X * topDiff.Y - bottomDiff.Y * topDiff.X) / determinant;
            var persp1 = (rightDiff.Y * leftDiff.X - rightDiff.X * leftDiff.Y) / determinant;

            return new SKMatrix
            {
                ScaleX = persp0 * bottomRight.X + bottomDiff.X,
                SkewX = persp1 * topLeft.X - leftDiff.X,
                TransX = bottomLeft.X,
                SkewY = persp0 * bottomRight.Y + bottomDiff.Y,
                ScaleY = persp1 * topLeft.Y - leftDiff.Y,
                TransY = bottomLeft.Y,
                Persp0 = persp0,
                Persp1 = persp1,
                Persp2 = 1,
            };
        }

        private static SKMatrix MapNegativeXYSquareToGivenPoints(SKPoint topLeft, SKPoint topRight, SKPoint bottomLeft, SKPoint bottomRight)
        {
            var leftDiff = topLeft - bottomLeft;
            var topDiff = topLeft - topRight;

            var determinant = leftDiff.X * topDiff.Y - topDiff.X * leftDiff.Y;

            var bottomDiff = bottomLeft - bottomRight;
            var rightDiff = topRight - bottomRight;

            var persp0 = (topDiff.X * bottomDiff.Y - topDiff.Y * bottomDiff.X) / determinant;
            var persp1 = (leftDiff.Y * rightDiff.X - leftDiff.X * rightDiff.Y) / determinant;

            return new SKMatrix
            {
                ScaleX = persp0 * bottomLeft.X - bottomDiff.X,
                SkewX = persp1 * topRight.X - rightDiff.X,
                TransX = bottomRight.X,
                SkewY = persp0 * bottomLeft.Y - bottomDiff.Y,
                ScaleY = persp1 * topRight.Y - rightDiff.Y,
                TransY = bottomRight.Y,
                Persp0 = persp0,
                Persp1 = persp1,
                Persp2 = 1,
            };
        }
    }
}
