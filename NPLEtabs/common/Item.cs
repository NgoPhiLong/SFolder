using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NPL.SBIM.Rhi.App.Item
{
    /// <summary>
    /// Rhino Object
    /// </summary>
    public class RhinoObjectInfo
    {
        [JsonProperty("Object ID")]
        public string ObjectID { get; set; }

        [JsonProperty("Layer Name")]
        public string LayerName { get; set; }

        [JsonProperty("Object Category")]
        public string ObjectCategory { get; set; }

        [JsonProperty("Object Type")]
        public string ObjectType { get; set; }

        [JsonProperty("Object Attribute")]
        public Dictionary<string, string> ObjectAttribute { get; set; }

        [JsonProperty("Curve Start")]
        public string CurveStart { get; set; }

        [JsonProperty("Curve End")]
        public string CurveEnd { get; set; }

        [JsonProperty("Curve Length")]
        public double CurveLength { get; set; }

        [JsonProperty("Curve Domain")]
        public string CurveDomain { get; set; }

        [JsonProperty("Boundary")]
        public List<string> Boundary { get; set; }
    }
    //public class RhinoObjectInfo
    //{
    //    [JsonProperty("Object ID")]
    //    public string ObjectID { get; set; }

    //    [JsonProperty("Layer Name")]
    //    public string LayerName { get; set; }

    //    [JsonProperty("Object Category")]
    //    public string ObjectCategory { get; set; }

    //    [JsonProperty("Object Type")]
    //    public string ObjectType { get; set; }

    //    [JsonProperty("Object Attribute")]
    //    public Dictionary<string, object> ObjectAttribute { get; set; }

    //    [JsonProperty("Curve Start")]
    //    public string CurveStart { get; set; }

    //    [JsonProperty("Curve End")]
    //    public string CurveEnd { get; set; }

    //    [JsonProperty("Curve Length")]
    //    public double? CurveLength { get; set; }

    //    [JsonProperty("Curve Domain")]
    //    public string CurveDomain { get; set; }

    //    [JsonProperty("Boundary")]
    //    public List<string> Boundary { get; set; }
    //}
    /// <summary>
    /// Khớp JSON format: { "Objects": [...] }
    /// </summary>
    public class RhinoJsonRoot
    {
        public string UnitSystem { get; set; }   // <-- thêm
        public List<RhinoObjectInfo> Objects { get; set; }
    }

    /// <summary>
    /// For WPF
    /// </summary>
    public class LayerItem : INotifyPropertyChanged
    {
        private string _layername;
        private bool _isSelected;

        public string LayerName
        {
            get => _layername;
            set
            {
                if (_layername != value)
                {
                    _layername = value;
                    OnPropertyChanged(nameof(LayerName));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public class RhinoObjectTypeItem : INotifyPropertyChanged
    {
        private string _rhinotypename;
        private string _rhinogsaid;
        private bool _isSelected;

        public string RhinoTypeName
        {
            get => _rhinotypename;
            set
            {
                if (_rhinotypename != value)
                {
                    _rhinotypename = value;
                    OnPropertyChanged(nameof(RhinoTypeName));
                }
            }
        }

        public string RhinoGSAID
        {
            get => _rhinogsaid;
            set
            {
                if (_rhinogsaid != value)
                {
                    _rhinogsaid = value;
                    OnPropertyChanged(nameof(RhinoGSAID));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        [JsonProperty("Curve Start")]
        public string CurveStart { get; set; }

        [JsonProperty("Curve End")]
        public string CurveEnd { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class RhinoAttributeItem : INotifyPropertyChanged
    {
        private string _keyname;
        private bool _isSelected;

        public string KeyName
        {
            get => _keyname;
            set
            {
                if (_keyname != value)
                {
                    _keyname = value;
                    OnPropertyChanged(nameof(KeyName));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Log
    {
        public string ObjectID { get; set; }     // Rhino ID
        public string EtabsName { get; set; }    // ETABS name (cache)
        public string EtabsGUID { get; set; }    // 🔥 PRIMARY KEY
        public string Section { get; set; }

        public string Type { get; set; }         // FRAME / AREA
        public string Status { get; set; }       // Created / Updated / Failed...

        public string Message { get; set; }      // 🔥 debug (error reason)
        public DateTime Time { get; set; }       // 🔥 timestamp
    }
    public static class SyncStatus
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Deleted = "Deleted";
        public const string Skipped = "Skipped";
        public const string Failed = "Failed";
    }
    public static class SyncType
    {
        public const string Frame = "FRAME";
        public const string Area = "AREA"; // slab + wall
    }
    public class FrameSyncItem
    {
        public string RhinoObjectId { get; set; }
        public string EtabsName { get; set; }
        public string EtabsGUID { get; set; }

        public XYZ Start { get; set; }
        public XYZ End { get; set; }

        public string Section { get; set; }
    }
    public class XYZ
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public XYZ() { }

        public XYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
    /// <summary>
    /// Revit Elements
    /// </summary>
    /// 
}
