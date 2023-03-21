using System;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;

namespace FlowArrows
{

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FlowArrows : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)

        {
            //Get Application as document objects
            UIApplication uiApplication = commandData.Application;
            Application application = uiApplication.Application;
            UIDocument uiDocument = uiApplication.ActiveUIDocument;
            Document document = uiApplication.ActiveUIDocument.Document;

            // Select the Flow Arrow to be used flowArrow
            FamilySymbol flowArrow = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fam => fam.Family.Name.Contains("Flow Arrow"));

            Transaction transaction = new Transaction(document);
            
            Reference reference = uiDocument.Selection.PickObject(ObjectType.PointOnElement);
            Pipe pipe = document.GetElement(reference) as Pipe;
            //Curve pipeCurve = ((LocationCurve)pipe.Location).Curve;
            Line line = (pipe.Location as LocationCurve).Curve as Line;
            ElementId levelId = pipe.LevelId;
            XYZ pipeDirection = line.Direction;
            XYZ point = reference.GlobalPoint;
            Level level = document.ActiveView.GenLevel;

            if (level == null)
                {
                    return Result.Failed;
                }
            try
            {
                transaction.Start("Flow Arrow");

                if (!flowArrow.IsActive)
                {
                    flowArrow.Activate();
                    document.Regenerate();
                }
                //Code to create flow arrow on the Curve of the pipe
                var placedArrow = document.Create.NewFamilyInstance(point, flowArrow, pipeDirection, document.GetElement(levelId) as Level, StructuralType.NonStructural);
                
                //Code to correct the insertion elevation of the object
                document.Regenerate();
                LocationPoint arrowLocationPoint = placedArrow.Location as LocationPoint;
                XYZ location = arrowLocationPoint.Point;
                XYZ differencePoint = new XYZ(point.X - location.X, point.Y - location.Y, point.Z - location.Z);
                placedArrow.Location.Move(new XYZ(differencePoint.X, differencePoint.Y, differencePoint.Z));

                //Rotate to match Pipe curve
                //XYZ pipeStartPoint = pipeCurve.GetEndPoint(0);
                //XYZ pipeEndPoint = pipeCurve.GetEndPoint(1);
                //XYZ pipeDirection = (pipeEndPoint - pipeStartPoint).Normalize();
                ////XYZ arrowDirection = XYZ.BasisZ;
                //Line rotationAxis = Line.CreateBound(pipeStartPoint, pipeEndPoint);
                //double rotationAngle = pipeCurve.ComputeDerivatives(0, true).BasisZ.AngleOnPlaneTo(differencePoint, pipeDirection);
                ////placedArrow.Location.Rotate(rotationAxis, rotationAngle);
                //ElementTransformUtils.RotateElement(document, placedArrow.Id, rotationAxis, rotationAngle);


                transaction.Commit();

            }
            //Ends Command if user Right clicks or presses Esc
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            //Catch all other exceptions errors
            catch (Exception ex)
            {
                message = ex.Message + " " + ex.StackTrace + " " + ex.Data + " " + ex.HResult;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}
