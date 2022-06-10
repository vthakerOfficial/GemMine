using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

//these datapoints represent behavioral events
//data about the event is currently stored in a dictionary
public class DataPoint
{
    private string type;
    private Dictionary<string, object> dataDict;
    private System.DateTime time;



    /// <summary>
    /// Initializes a new instance of the <see cref="T:DataPoint"/> class.  This represents a piece of data that you might want to keep about your project.
    /// 
    /// "Type" is a short description of the data.  Time is the time when the datapoint occured (or was collected, if it's a continuous event).
    /// 
    /// dataDict contains the actual data that you might want to analyze later.  Each element of the data is a key-value pair, the key representing its name.
    /// The value can be any C# object.  If the datapoint is written to disk using a WirteToDiskHandler, the handler will try to deduce an appropriate way of
    /// serializing the object.  This is easy for strings, integers, booleans, etc., but for other objects the object's ToString method might be used as a fallback.
    /// </summary>
    /// <param name="newType">New type.</param>
    /// <param name="newTime">New time.</param>
    /// <param name="newDataDict">New data dict.</param>
    public DataPoint(string newType, System.DateTime newTime, Dictionary<string, object> newDataDict)
    {
        if (newDataDict == null)
            newDataDict = new Dictionary<string, object>();

        type = newType;
        dataDict = newDataDict;
        time = newTime;
    }

    /// <summary>
    /// Returns a JSON string representing this datapoint.
    /// 
    /// Strings conforming to certain formats will be converted to corresponding types.  For example, if a string looks like a number it will be represented as a JSON number type. 
    /// </summary>
    /// <returns>The json.</returns>
    public string ToJSON()
    {
        double unixTimestamp = ConvertToMillisecondsSinceEpoch(time);

        var dataPointjson = new Dictionary<string, object> {
            { "type", type },
            { "time", unixTimestamp.ToString() },
            { "data", dataDict } };
        return JsonConvert.SerializeObject(dataPointjson);
    }

    public static double ConvertToMillisecondsSinceEpoch(System.DateTime convertMe)
    {
        double milliseconds = (double)(convertMe.ToUniversalTime().Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))).TotalMilliseconds;
        return milliseconds;
    }
}