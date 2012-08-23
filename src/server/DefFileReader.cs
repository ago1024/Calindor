using System;
using System.Collections;
using System.Text;
using System.IO;

namespace Calindor.Server
{
    public class DefFileReader
    {
        enum StateType { Start, General, TeleportPoint, SoundArea, UseArea, LocationInfo, ObjectName, TextArea, InterdictionArea, Attributes, AttributeTag };

        public static MapDefinition.MapEntry readDefFile(string deffile)
        {
            StreamReader stream;
            StateType state = StateType.Start;
            string line;
            string sectionName = null;
            char[] whiteSpaces = " \t\r\n".ToCharArray();

            MapDefinition.MapEntry ret = new MapDefinition.MapEntry();

            IDictionary sectionProperties = new Hashtable();

            try
            {
                if (!File.Exists(deffile))
                    return null;
                stream = new StreamReader(deffile);
            }
            catch (IOException)
            {
                return null;
            }


            int lineNo = 0;
            // Loop all lines until the end of the file
            while ((line = stream.ReadLine()) != null)
            {
                bool isTag = false;
                bool isEndTag = false;
                bool isKeyValue = false;
                string tag = null;
                string key = null;
                string value = null;
                lineNo++;

                // Prepare the string:
                // trim whitespaces
                line.Trim(whiteSpaces);

                // check if it is a section tag
                if (line.StartsWith("[") && line.Contains("]"))
                {
                    int start = line.IndexOf("[") + 1;
                    int end = line.IndexOf("]");
                    tag = line.Substring(start, end - start);
                    isTag = true;
                }

                // check if the tag closes the current section
                if (sectionName != null && isTag && tag == "/" + sectionName)
                    isEndTag = true;

                // check if the line is a key-value pair and extract key and value
                if (!isTag && line.Contains(":"))
                {
                    int colon = line.IndexOf(":");
                    key = line.Substring(0, colon).Trim(whiteSpaces);
                    value = line.Substring(colon + 1).Trim(whiteSpaces);
                    isKeyValue = true;
                }

                // state machine.
                // there are basicly 4 states:
                // Start  <-----------> Attributes  <------------> AttributeTag
                //        <-----------> Other sections
                //
                // The other sections are split into separate states but they are handled the same way
                // because they are very similar
                //
                switch (state)
                {
                    case StateType.Start:
                        // empty the section properties
                        //sectionProperties.Clear();
                        sectionProperties = new Hashtable();
                        switch (tag)
                        {
                            case ("general"):
                                state = StateType.General;
                                sectionName = tag;
                                break;
                            case ("teleport_point"):
                                state = StateType.TeleportPoint;
                                sectionName = tag;
                                break;
                            case ("interdiction_area"):
                                state = StateType.InterdictionArea;
                                sectionName = tag;
                                break;
                            case ("text_area"):
                                state = StateType.TextArea;
                                sectionName = tag;
                                break;
                            case ("object_name"):
                                state = StateType.ObjectName;
                                sectionName = tag;
                                break;
                            case ("location_info"):
                                state = StateType.LocationInfo;
                                sectionName = tag;
                                break;
                            case ("use_area"):
                                state = StateType.UseArea;
                                sectionName = tag;
                                break;
                            case ("sound_area"):
                                state = StateType.SoundArea;
                                sectionName = tag;
                                break;
                            case ("attributes"):
                                state = StateType.Attributes;
                                sectionName = tag;
                                break;
                            default:
                                // ignore other tags
                                Console.WriteLine("Unhandled tag: Section Start: " + line);
                                break;
                            case null:
                                // ignore non-tags aka comments
                                break;
                        }
                        break;
                    case StateType.Attributes:
                        // empty the section properties
                        sectionProperties.Clear();
                        if (isEndTag)
                        {
                            state = StateType.Start;
                            sectionName = null;
                        }
                        else if (isTag)
                        {
                            state = StateType.AttributeTag;
                            sectionName = tag;
                        }
                        else
                        {
                            Console.WriteLine("Unhandled: Section " + sectionName + ": " + line);
                            // ignore comments
                        }
                        break;
                    case StateType.AttributeTag:
                        if (isEndTag)
                        {
                            if (sectionProperties.Contains("min_x") && sectionProperties.Contains("max_x"))
                                if (Int16.Parse((string)sectionProperties["min_x"]) >= Int16.Parse((string)sectionProperties["max_x"]))
                                    throw new Exception(String.Format("Misplaced min/max value ({0},{1}) in line {2} section {3}", Int16.Parse((string)sectionProperties["min_x"]), Int16.Parse((string)sectionProperties["max_x"]), lineNo, sectionName));
                            if (sectionProperties.Contains("min_y") && sectionProperties.Contains("max_y"))
                                if (Int16.Parse((string)sectionProperties["min_y"]) >= Int16.Parse((string)sectionProperties["max_y"]))
                                    throw new Exception(String.Format("Misplaced min/max value ({0},{1}) in line {2} section {3}", Int16.Parse((string)sectionProperties["min_y"]), Int16.Parse((string)sectionProperties["max_y"]), lineNo, sectionName));

                            ret.Add(MapDefinition.AttributeArea.Create(sectionProperties, sectionName));

                            state = StateType.Attributes;
                            sectionName = "attributes";
                        }
                        else if (isKeyValue)
                        {
                            sectionProperties.Add(key, value);
                        }
                        else
                        {
                            Console.WriteLine("Unhandled: Section " + sectionName + ": " + line);
                            // ignore everything else
                        }
                        break;
                    default:
                        if (isEndTag)
                        {
                            if (sectionProperties.Contains("min_x") && sectionProperties.Contains("max_x"))
                                if (Int16.Parse((string)sectionProperties["min_x"]) >= Int16.Parse((string)sectionProperties["max_x"]))
                                    throw new Exception(String.Format("Misplaced min/max value ({0},{1}) in line {2} section {3}", Int16.Parse((string)sectionProperties["min_x"]), Int16.Parse((string)sectionProperties["max_x"]), lineNo, sectionName));
                            if (sectionProperties.Contains("min_y") && sectionProperties.Contains("max_y"))
                                if (Int16.Parse((string)sectionProperties["min_y"]) >= Int16.Parse((string)sectionProperties["max_y"]))
                                    throw new Exception(String.Format("Misplaced min/max value ({0},{1}) in line {2} section {3}", Int16.Parse((string)sectionProperties["min_y"]), Int16.Parse((string)sectionProperties["max_y"]), lineNo, sectionName));

                            switch (sectionName)
                            {
                                case "use_area":
                                    ret.Add(MapDefinition.UseArea.Create(sectionProperties));
                                    break;
                                case "object_name":
                                    ret.Add(MapDefinition.ObjectName.Create(sectionProperties));
                                    break;
                                case "text_area":
                                    ret.Add(MapDefinition.TextArea.Create(sectionProperties));
                                    break;
                                case "teleport_point":
                                    ret.Add(MapDefinition.TeleportPoint.Create(sectionProperties));
                                    break;
                            }

                            state = StateType.Start;
                            sectionName = null;
                        }
                        else if (isKeyValue)
                        {
                            sectionProperties.Add(key, value);
                        }
                        else
                        {
                            Console.WriteLine("Unhandled: Section " + sectionName + ": " + line);
                            // ignore everything else
                        }
                        break;
                }
            }
            return ret;
        }
    }
}
