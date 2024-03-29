﻿using System;
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


            //Select the Flow Arrow to be used flowArrow
            FamilySymbol flowArrow = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fam => fam.Family.Name.Contains("Flow Arrow"));


            //Select the pipe or line based Item
            Reference reference = uiDocument.Selection.PickObject(ObjectType.PointOnElement);
            Element pipe = document.GetElement(reference);
            Line line = (pipe.Location as LocationCurve).Curve as Line;
            ElementId levelId = pipe.LevelId;
            XYZ pipeDirection = line.Direction;
            XYZ point = reference.GlobalPoint;

            Transaction transaction = new Transaction(document);
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
