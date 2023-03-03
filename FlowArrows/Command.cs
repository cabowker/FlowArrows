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

            Transaction transaction = new Transaction(document);

            // Select the Flow Arrow to be used flowArrow
            FamilySymbol flowArrow = new FilteredElementCollector(document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fam => fam.Family.Name.Contains("Flow Arrow"));



                Reference reference = uiDocument.Selection.PickObject(ObjectType.PointOnElement);
                Pipe pipe = document.GetElement(reference) as Pipe;
                ElementId levelId = pipe.LevelId;
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

                document.Create.NewFamilyInstance(point, flowArrow, document.GetElement(levelId) as Level, StructuralType.NonStructural);
                
                document.Regenerate();
                XYZ arrowOrigin = reference.GlobalPoint;
                XYZ differencePoint = new XYZ(point.X - arrowOrigin.X, point.Y - arrowOrigin.Y, point.Z - arrowOrigin.Z);
                flowArrow.Location.Move(new XYZ(differencePoint.X, differencePoint.Y, differencePoint.Z));


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
