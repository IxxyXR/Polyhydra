using System;using Newtonsoft.Json.Converters;


// Empty subclass
// Hopefully fix runtime errors on WebGL build
// https://stackoverflow.com/questions/37776866/json-net-8-0-error-creating-stringenumconverter-on-mono-4-5-mac

public class MyStringEnumConverter:StringEnumConverter
{
    
}
