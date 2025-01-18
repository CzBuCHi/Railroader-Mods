using System.Linq.Expressions;
using Core;
using HarmonyLib;
using Serilog;

namespace MapEditor.Utility;

public static class IdGenerators
{
    private static string _Prefix = "Custom_";

    public static string Prefix
    {
        get => _Prefix;
        set
        {
            Log.Information("PrefixChanged: " + value);
            _Prefix = value;
            _TrackNodes = null;
            _TrackSegments = null;
            _TrackSpans = null;
            _TrackMarkers = null;
            _Cars = null;
            _TrainCrew = null;
        }
    }

    private static readonly IdGeneratorFactoryDelegate _IdGeneratorFactory = BuildIdGeneratorFactory();

    private static IdGenerator? _TrackNodes;
    public static IdGenerator TrackNodes => _TrackNodes ??= _IdGeneratorFactory("N" + _Prefix, 3);

    private static IdGenerator? _TrackSegments;
    public static IdGenerator TrackSegments => _TrackSegments ??= _IdGeneratorFactory("S" + _Prefix, 3);

    private static IdGenerator? _TrackSpans;
    public static IdGenerator TrackSpans => _TrackSpans ??= _IdGeneratorFactory("P" + _Prefix, 3);

    private static IdGenerator? _TrackMarkers;
    public static IdGenerator TrackMarkers => _TrackMarkers ??= _IdGeneratorFactory("M" + _Prefix, 3);

    private static IdGenerator? _Cars;
    public static IdGenerator Cars => _Cars ??= _IdGeneratorFactory("C" + _Prefix, 3);

    private static IdGenerator? _TrainCrew;
    public static IdGenerator TrainCrew => _TrainCrew ??= _IdGeneratorFactory("T" + _Prefix, 3);

    private static IdGenerator? _Scenery;
    public static  IdGenerator  Scenery => _Scenery ??= _IdGeneratorFactory("SC" + _Prefix, 3);

    private delegate IdGenerator IdGeneratorFactoryDelegate(string prefix, int digits);

    private static IdGeneratorFactoryDelegate BuildIdGeneratorFactory()
    {
        var constructor = AccessTools.Constructor(typeof(IdGenerator), [typeof(string), typeof(int)])!;
        var prefix = Expression.Parameter(typeof(string), "prefix");
        var digits = Expression.Parameter(typeof(int), "digits");
        var instance = Expression.New(constructor, prefix, digits);
        var result = Expression.Convert(instance, typeof(IdGenerator));
        var lambda = Expression.Lambda<IdGeneratorFactoryDelegate>(result, prefix, digits);
        return lambda.Compile();
    }
}
