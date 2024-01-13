using NXOpen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcAndLineIntersection
{
    public class Intersect
    {
        public static void Main(string[] args)
        {
            #region Basic need

            Session theSession = Session.GetSession();
            UI theUI = UI.GetUI();

            FileNew newFile = theSession.Parts.FileNew();
            newFile.Units = Part.Units.Millimeters;
            newFile.TemplateFileName = "model-plain-1-mm-template.prt";
            newFile.NewFileName = MakeUnique(@"C:\Users\saiku\OneDrive\Desktop\Test NX files\Intersection.prt");
            newFile.Commit();

            Part workPart = theSession.Parts.Work;
            ListingWindow lw = theSession.ListingWindow;
            lw.Open();

            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            Vector3d normal_X1 = new Vector3d(1.0, 0.0, 0.0);
            Vector3d normal_Y1 = new Vector3d(0.0, 1.0, 0.0);
            Vector3d normal_Z1 = new Vector3d(0.0, 0.0, 1.0);

            DatumAxis datumAxis_X = (DatumAxis)workPart.Datums.FindObject("DATUM_CSYS(0) X axis");
            DatumAxis datumAxis_Y = (DatumAxis)workPart.Datums.FindObject("DATUM_CSYS(0) Y axis");
            //DatumAxis datumAxis_Z = (DatumAxis)workPart.Datums.FindObject("DATUM_CSYS(0) Z axis");

            NXMatrix nXMatrix;

            #endregion Basic need

            #region Sketch

            SketchInPlaceBuilder sktBldr_shroud = SketchOn_XY_planeMethod();
            Sketch skt_shroud = (Sketch)sktBldr_shroud.Commit();
            sktBldr_shroud.Destroy();
            skt_shroud.Activate(Sketch.ViewReorient.False);

            double radius = 100;
            double startAngle = DegreesToRadians(190);
            double endAngle = DegreesToRadians(310);
            Point3d startPt = new Point3d(-20, 20, 0);
            Point3d endPt = new Point3d(50, -210, 0);

            nXMatrix = theSession.ActiveSketch.Orientation;
            NXOpen.Arc arc = workPart.Curves.CreateArc(origin, nXMatrix, radius, startAngle, endAngle);
            NXOpen.Line line = workPart.Curves.CreateLine(startPt, endPt);

            skt_shroud.AddGeometry(arc);
            skt_shroud.AddGeometry(line);

            skt_shroud.Deactivate(Sketch.ViewReorient.False, Sketch.UpdateLevel.SketchOnly);
            Point3d[] point3Ds = CircleAndLineIntersections(line, arc);
            Point3d pt1 = point3Ds[0];
            Point3d pt2 = point3Ds[1];
            Echo(pt1.ToString());
            Echo(pt2.ToString());
            //[X= -70.7106781186547,Y = -70.7106781186547,Z = 0]

            #region
            bool lies = IsPointOnLine(line, pt1);
            if (lies)
            {
                Echo("pt1 lies on LINE");
            }
            else
            {
                Echo("pt1 not lies on LINE");
            }

            lies = IsPointOnLine(line, pt2);
            if (lies)
            {
                Echo("pt2 lies on LINE");
            }
            else
            {
                Echo("pt2 not lies on LINE");
            }

            lies = IsPointOnLine(line, origin);
            if (lies)
            {
                Echo("origin lies on LINE");
            }
            else
            {
                Echo("origin not lies on LINE");
            }
            double angle = AngleOfTwoPointsInD(startPt, endPt);

            //Echo(angle.ToString());
            bool arcpt;
            arcpt = IsPointOnArc(pt1, arc);
            if (arcpt)
            {
                Echo("pt1 lies on ARC");
            }
            else
            {
                Echo("pt1 not lies on ARC");
            }
            #endregion Sketch

            arcpt = IsPointOnArc(pt2, arc);
            if (arcpt)
            {
                Echo("pt2 lies on ARC");
            }
            else
            {
                Echo("pt2 not lies on ARC");
            }

            #endregion Sketch
        }

        public static int GetUnloadOption(string dummy)
        {
            //return (int)Session.LibraryUnloadOption.Explicitly;
            return (int)Session.LibraryUnloadOption.Immediately;
        }

        public static string MakeUnique(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            int i = 0;
            while (File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);
            }

            return fileName;
        }

        public static SketchInPlaceBuilder SketchOn_XY_planeMethod()
        {
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;
            Vector3d normal_X1 = new Vector3d(1.0, 0.0, 0.0);
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            Matrix3x3 matrix_XY = new Matrix3x3
            {
                Xx = 0,
                Xy = 1,
                Xz = 0,
                Yx = 1,
                Yy = 0,
                Yz = 0,
                Zx = 0,
                Zy = 0,
                Zz = -1
            };

            Direction direction = workPart.Directions.CreateDirection(workPart.Points.CreatePoint(origin), normal_X1);
            Plane plane = workPart.Planes.CreateFixedTypePlane(origin, matrix_XY, SmartObject.UpdateOption.WithinModeling);
            Xform xform = workPart.Xforms.CreateXformByPlaneXDirPoint(plane, direction, workPart.Points.CreatePoint(origin), SmartObject.UpdateOption.WithinModeling, 0.625, false, true);
            CartesianCoordinateSystem ccSystem = workPart.CoordinateSystems.CreateCoordinateSystem(xform, SmartObject.UpdateOption.WithinModeling);

            Sketch sketch = null;
            SketchInPlaceBuilder sktBuilder = workPart.Sketches.CreateNewSketchInPlaceBuilder(sketch);
            sktBuilder.Csystem = ccSystem;
            sktBuilder.PlaneOption = Sketch.PlaneOption.Inferred;
            sktBuilder.AxisOrientation = AxisOrientation.Horizontal;
            sktBuilder.OriginOption = OriginMethod.SpecifyPoint;
            return sktBuilder;
        }

        public static SketchInPlaceBuilder SketchOn_XZ_planeMethod()
        {
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;
            Vector3d normal_X1 = new Vector3d(1.0, 0.0, 0.0);
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            Matrix3x3 matrix_XZ = new Matrix3x3
            {
                Xx = 1,
                Xy = 0,
                Xz = 0,
                Yx = 0,
                Yy = 0,
                Yz = -1,
                Zx = 0,
                Zy = 1,
                Zz = 0
            };

            Direction direction = workPart.Directions.CreateDirection(workPart.Points.CreatePoint(origin), normal_X1);
            Plane plane = workPart.Planes.CreateFixedTypePlane(origin, matrix_XZ, SmartObject.UpdateOption.WithinModeling);
            Xform xform = workPart.Xforms.CreateXformByPlaneXDirPoint(plane, direction, workPart.Points.CreatePoint(origin), SmartObject.UpdateOption.WithinModeling, 0.625, false, true);
            CartesianCoordinateSystem ccSystem = workPart.CoordinateSystems.CreateCoordinateSystem(xform, SmartObject.UpdateOption.WithinModeling);

            Sketch sketch = null;
            SketchInPlaceBuilder sktBuilder = workPart.Sketches.CreateNewSketchInPlaceBuilder(sketch);
            sktBuilder.Csystem = ccSystem;
            sktBuilder.PlaneOption = Sketch.PlaneOption.Inferred;
            sktBuilder.AxisOrientation = AxisOrientation.Horizontal;
            sktBuilder.OriginOption = OriginMethod.SpecifyPoint;
            return sktBuilder;
        }

        public static SketchInPlaceBuilder SketchOn_YZ_planeMethod()
        {
            Session theSession = Session.GetSession();
            Part workPart = theSession.Parts.Work;
            Vector3d normal_Y1 = new Vector3d(0.0, 1.0, 0.0);
            Point3d origin = new Point3d(0.0, 0.0, 0.0);
            Matrix3x3 matrix_YZ = new Matrix3x3
            {
                Xx = 0,
                Xy = 0,
                Xz = 1,
                Yx = 0,
                Yy = 1,
                Yz = 0,
                Zx = -1,
                Zy = 0,
                Zz = 0
            };

            Direction direction = workPart.Directions.CreateDirection(workPart.Points.CreatePoint(origin), normal_Y1);
            Plane plane = workPart.Planes.CreateFixedTypePlane(origin, matrix_YZ, SmartObject.UpdateOption.WithinModeling);
            Xform xform = workPart.Xforms.CreateXformByPlaneXDirPoint(plane, direction, workPart.Points.CreatePoint(origin), SmartObject.UpdateOption.WithinModeling, 0.625, false, true);
            CartesianCoordinateSystem ccSystem = workPart.CoordinateSystems.CreateCoordinateSystem(xform, SmartObject.UpdateOption.WithinModeling);

            Sketch sketch = null;
            SketchInPlaceBuilder sktBuilder = workPart.Sketches.CreateNewSketchInPlaceBuilder(sketch);
            sktBuilder.Csystem = ccSystem;
            sktBuilder.PlaneOption = Sketch.PlaneOption.Inferred;
            sktBuilder.AxisOrientation = AxisOrientation.Horizontal;
            sktBuilder.OriginOption = OriginMethod.SpecifyPoint;
            return sktBuilder;
        }

        public static double DegreesToRadians(double x)
        {
            double value = x * (Math.PI / 180);
            return value;
        }

        public static double RadiansToDegrees(double x)
        {
            double value = x * (180 / Math.PI);
            return value;
        }

        public static double AngleOfTwoPointsInR(Point3d point1, Point3d point2)
        {
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            /*

            If two points are (x1,y1) & (x2,y2)
            then the angle is ( lets assume  "θ" )
            θ = arctan(y2 - y1) / (x2 - x1)

             */
            if (point1.X is 0 && point2.X is 0)
            {
                x1 = point1.Y;
                y1 = point1.Z;
                x2 = point2.Y;
                y2 = point2.Z;
            }
            else if (point1.Y is 0 && point2.Y is 0)
            {
                x1 = point1.X;
                y1 = point1.Z;
                x2 = point2.X;
                y2 = point2.Z;
            }
            else if (point1.Z is 0 && point2.Z is 0)
            {
                x1 = point1.X;
                y1 = point1.Y;
                x2 = point2.X;
                y2 = point2.Y;
            }
            double angle = Math.Atan((y2 - y1) / (x2 - x1));
            return angle;
        }

        public static double AngleOfTwoPointsInD(Point3d point1, Point3d point2)
        {
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            /*

            If two points are (x1,y1) & (x2,y2)
            then the angle is ( lets assume  "θ" )
            θ = arctan(y2 - y1) / (x2 - x1)

             */
            if (point1.X is 0 && point2.X is 0)
            {
                x1 = point1.Y;
                y1 = point1.Z;
                x2 = point2.Y;
                y2 = point2.Z;
            }
            else if (point1.Y is 0 && point2.Y is 0)
            {
                x1 = point1.X;
                y1 = point1.Z;
                x2 = point2.X;
                y2 = point2.Z;
            }
            else if (point1.Z is 0 && point2.Z is 0)
            {
                x1 = point1.X;
                y1 = point1.Y;
                x2 = point2.X;
                y2 = point2.Y;
            }
            double angle = Math.Atan((y2 - y1) / (x2 - x1));
            angle *= (180 / Math.PI);
            return angle;
        }

        public static double LengthOfLine(Point3d point1, Point3d point2)
        {
            double x1 = point1.X;
            double y1 = point1.Y;
            double z1 = point1.Z;
            double x2 = point2.X;
            double y2 = point2.Y;
            double z2 = point2.Z;
            double A = Math.Pow(x2 - x1, 2);
            double B = Math.Pow(y2 - y1, 2);
            double C = Math.Pow(z2 - z1, 2);
            double length = Math.Sqrt(A + B + C);
            return length;
        }

        public static void Echo(string output)
        {
            Session theSession = Session.GetSession();
            theSession.ListingWindow.Open();
            theSession.ListingWindow.WriteLine(output);
            theSession.LogFile.WriteLine(output);
        }

        public static Point3d[] CircleAndLineIntersections(Line line, Arc arc)
        {
            Point3d lineStartPoint = line.StartPoint;
            Point3d lineEndPoint = line.EndPoint;
            Point3d circleCenterPoint = arc.CenterPoint;
            double radius = arc.Radius;
            double x1, y1, z1, x2, y2, z2, X, Y, Z;
            x1 = lineStartPoint.X; y1 = lineStartPoint.Y; z1 = lineStartPoint.Z;
            x2 = lineEndPoint.X; y2 = lineEndPoint.Y; z2 = lineEndPoint.Z;
            X = circleCenterPoint.X; Y = circleCenterPoint.Y; Z = circleCenterPoint.Z;

            // Calculate the direction vector of the line
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;

            // Calculate the coefficients for the quadratic equation
            double a = dx * dx + dy * dy + dz * dz;
            double b = 2 * (dx * (x1 - X) + dy * (y1 - Y) + dz * (z1 - Z));
            double c = (x1 - X) * (x1 - X) + (y1 - Y) * (y1 - Y) + (z1 - Z) * (z1 - Z) - radius * radius;

            // Calculate the discriminant
            double discriminant = b * b - 4 * a * c;

            // Check if there are real solutions
            if (discriminant < 0)
            {
                Console.WriteLine("No real intersection points.");
                return new Point3d[0];
            }

            // Calculate the parameter values for the intersection points
            double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
            double t2 = (-b - Math.Sqrt(discriminant)) / (2 * a);

            // Calculate the intersection points
            Point3d[] intersections = {
            new Point3d(x1 + t1 * dx, y1 + t1 * dy, z1 + t1 * dz),
            new Point3d(x1 + t2 * dx, y1 + t2 * dy, z1 + t2 * dz) };

            return intersections;
        }

        public static bool IsPointOnRay(Line line, Point3d point)
        {
            double X = point.X;
            double Y = point.Y;
            double Z = point.Z;
            double x1 = line.StartPoint.X;
            double y1 = line.StartPoint.Y;
            double z1 = line.StartPoint.Z;
            double x2 = line.EndPoint.X;
            double y2 = line.EndPoint.Y;
            double z2 = line.EndPoint.Z;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;

            double ex = X - x1;
            double ey = Y - y1;
            double ez = Z - z1;

            double q = dx * ex;
            q += dy * ey;
            q += dz * ey;
            q *= q;
            q /= (dx * dx + dy * dy + dz * dz);
            q /= (ex * ex + ey * ey + ez * ez);

            if (q >= 1.0 - 1e-10)
            {
                return true;
            }
            return false;
            //           point p(x, y) is on the line
            //else p(x, y) is not on line
        }

        public static Point3d ArcStartPoint(NXOpen.Arc arc)
        {
            Point3d centerPoint = arc.CenterPoint;
            double radius = arc.Radius;
            double startAngle = arc.StartAngle;

            double x = 0;
            double y = 0;
            double z = 0;

            if (centerPoint.X is 0)
            {
                y = centerPoint.Y + radius * Math.Cos(startAngle);
                z = centerPoint.Z + radius * Math.Sin(startAngle);
            }
            else if (centerPoint.Y is 0)
            {
                x = centerPoint.X + radius * Math.Cos(startAngle);
                z = centerPoint.Z + radius * Math.Sin(startAngle);
            }
            else if (centerPoint.Z is 0)
            {
                x = centerPoint.X + radius * Math.Cos(startAngle);
                y = centerPoint.Y + radius * Math.Sin(startAngle);
            }

            Point3d startPoint = new Point3d(x, y, z);

            return startPoint;
        }

        public static Point3d ArcEndPoint(NXOpen.Arc arc)
        {
            Point3d centerPoint = arc.CenterPoint;
            double radius = arc.Radius;
            double endAngle = arc.EndAngle;

            double x = 0;
            double y = 0;
            double z = 0;

            if (centerPoint.X is 0)
            {
                y = centerPoint.Y + radius * Math.Cos(endAngle);
                z = centerPoint.Z + radius * Math.Sin(endAngle);
            }
            else if (centerPoint.Y is 0)
            {
                x = centerPoint.X + radius * Math.Cos(endAngle);
                z = centerPoint.Z + radius * Math.Sin(endAngle);
            }
            else if (centerPoint.Z is 0)
            {
                x = centerPoint.X + radius * Math.Cos(endAngle);
                y = centerPoint.Y + radius * Math.Sin(endAngle);
            }

            Point3d startPoint = new Point3d(x, y, z);

            return startPoint;
        }

        public static Point3d ArcMidPoint(NXOpen.Arc arc)
        {
            Point3d centerPoint = arc.CenterPoint;
            double radius = arc.Radius;
            double midAngle = (arc.StartAngle + arc.EndAngle) / 2.0;

            double x = 0;
            double y = 0;
            double z = 0;

            if (centerPoint.X is 0)
            {
                y = centerPoint.Y + radius * Math.Cos(midAngle);
                z = centerPoint.Z + radius * Math.Sin(midAngle);
            }
            else if (centerPoint.Y is 0)
            {
                x = centerPoint.X + radius * Math.Cos(midAngle);
                z = centerPoint.Z + radius * Math.Sin(midAngle);
            }
            else if (centerPoint.Z is 0)
            {
                x = centerPoint.X + radius * Math.Cos(midAngle);
                y = centerPoint.Y + radius * Math.Sin(midAngle);
            }

            Point3d startPoint = new Point3d(x, y, z);

            return startPoint;
        }

        private static bool IsPointOnArc(Point3d point, Arc arc)
        {
            Point3d arccenterPoint = arc.CenterPoint;
            double radius = arc.Radius;

            // Check if the point is on the circle that the arc belongs to
            double distanceToPoint = LengthOfLine(point, arccenterPoint);
            if (Math.Abs(distanceToPoint - radius) > 0.00001)
            {
                // Point is not on the circle
                return false;
            }

            // Check if the point is within the angular range of the arc

            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;
            double pointAngle = AngleOfTwoPointsInR(point, arccenterPoint);

            // Adjust angles to be in the range [0, 2π)
            if (startAngle < 0)
            {
                startAngle += 2 * Math.PI;
            }
            if (endAngle < 0)
            {
                endAngle += 2 * Math.PI;
            }
            if (pointAngle < 0)
            {
                pointAngle += 2 * Math.PI;
            }

            pointAngle = RadiansToDegrees(pointAngle);
            if ((point.X <= 0 && point.Y >= 0 && point.Z == 0) || (point.X <= 0 && point.Y == 0 && point.Z >= 0) || (point.X == 0 && point.Y <= 0 && point.Z >= 0))
            {
                pointAngle = pointAngle + 180 - 360;
            }
            if ((point.X <= 0 && point.Y <= 0 && point.Z == 0) || (point.X <= 0 && point.Y == 0 && point.Z <= 0) || (point.X == 0 && point.Y <= 0 && point.Z <= 0))
            {
                pointAngle += 180;
            }
            pointAngle = DegreesToRadians(pointAngle);

            // Check if the point angle is within the arc's angular range
            if (startAngle < endAngle)
            {
                if (pointAngle >= startAngle && pointAngle <= endAngle)
                {
                    //Echo("pointAngle is: " + RadiansToDegrees(pointAngle).ToString());
                    //Echo("startAngle is: " + RadiansToDegrees(startAngle).ToString());
                    //Echo("endAngle is: " + RadiansToDegrees(endAngle).ToString());
                    return true;
                }
            }

            double angle = endAngle;
            if (RadiansToDegrees(endAngle) > 360)
            {
                angle = DegreesToRadians(RadiansToDegrees(endAngle) - 360);
            }

            if (startAngle > angle)
            {
                if ((pointAngle >= startAngle && pointAngle <= endAngle) || (pointAngle <= endAngle))
                {
                    //Echo("pointAngle is: " + RadiansToDegrees(pointAngle).ToString());
                    //Echo("startAngle is: " + RadiansToDegrees(startAngle).ToString());
                    //Echo("endAngle is: " + RadiansToDegrees(endAngle).ToString());
                    return true;
                }
            }
            Echo("pointAngle is: " + RadiansToDegrees(pointAngle).ToString());
            Echo("startAngle is: " + RadiansToDegrees(startAngle).ToString());
            Echo("endAngle is: " + RadiansToDegrees(endAngle).ToString());
            return false;
        }

        public static bool IsPointOnLine(Line line, Point3d point)
        {
            double X = point.X;
            double Y = point.Y;
            double Z = point.Z;
            double x1 = line.StartPoint.X;
            double y1 = line.StartPoint.Y;
            double z1 = line.StartPoint.Z;
            double x2 = line.EndPoint.X;
            double y2 = line.EndPoint.Y;
            double z2 = line.EndPoint.Z;
            double epsilon = 1e-6; // A small value to handle floating-point precision errors

            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;

            double ex = X - x1;
            double ey = Y - y1;
            double ez = Z - z1;

            double q = dx * ex;
            q += dy * ey;
            q += dz * ey;
            q *= q;
            q /= (dx * dx + dy * dy + dz * dz);
            q /= (ex * ex + ey * ey + ez * ez);
            bool lies = false;
            if (q >= 1.0 - 1e-10)
            {
                lies = true;
            }
            //           point p(x, y) is on the line
            //else p(x, y) is not on line
            //if (lies)
            //{
            //    if (x1 <= 0 && y1 <= 0 && z1 <= 0 && x2 <= 0 && y2 <= 0 && z2 <= 0)
            //    {
            //        if (x1 <= X && y1 <= Y && z1 <= Z && x2 >= X && y2 >= Y && z2 >= Z)
            //        {
            //            return true;
            //        }
            //    }
            //}
            if (lies)
            {
                //(x1,y1,z1) are all <
                if ((x1 <= x2 && y1 <= y2 && z1 <= z2))
                {
                    if (x1 <= X && X <= x2)
                    {
                        if (y1 <= Y && Y <= y2)
                        {
                            if (z1 <= Z && Z <= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                //(x1,y1,z1) are all >
                if ((x1 >= x2 && y1 >= y2 && z1 >= z2))
                {
                    if (x1 >= X && X >= x2)
                    {
                        if (y1 >= Y && Y >= y2)
                        {
                            if (z1 >= Z && Z >= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (x1) >
                if ((x1 >= x2 && y1 <= y2 && z1 <= z2))
                {
                    if (x1 >= X && X >= x2)
                    {
                        if (y1 <= Y && Y <= y2)
                        {
                            if (z1 <= Z && Z <= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (y1) >
                if ((x1 <= x2 && y1 >= y2 && z1 <= z2))
                {
                    if (x1 <= X && X <= x2)
                    {
                        if (y1 >= Y && Y >= y2)
                        {
                            if (z1 <= Z && Z <= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (z1) >
                if ((x1 <= x2 && y1 <= y2 && z1 >= z2))
                {
                    if (x1 <= X && X <= x2)
                    {
                        if (y1 <= Y && Y <= y2)
                        {
                            if (z1 >= Z && Z >= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (x1,y1) >
                if ((x1 >= x2 && y1 >= y2 && z1 <= z2))
                {
                    if (x1 >= X && X >= x2)
                    {
                        if (y1 >= Y && Y >= y2)
                        {
                            if (z1 <= Z && Z <= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (x1,z1) >
                if ((x1 >= x2 && y1 <= y2 && z1 >= z2))
                {
                    if (x1 >= X && X >= x2)
                    {
                        if (y1 <= Y && Y <= y2)
                        {
                            if (z1 >= Z && Z >= z2)
                            {
                                return true;
                            }
                        }
                    }
                }

                // (y1,z1) >
                if ((x1 <= x2 && y1 >= y2 && z1 >= z2))
                {
                    if (x1 <= X && X <= x2)
                    {
                        if (y1 >= Y && Y >= y2)
                        {
                            if (z1 >= Z && Z >= z2)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}