using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace OVCMOVE2026.Plugin.Common;

public sealed class FlexibleDateTimeSerializer : SerializerBase<DateTime>
{
    public override DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.CurrentBsonType;
        switch (type)
        {
            case BsonType.DateTime:
                return BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(context.Reader.ReadDateTime());
            case BsonType.String:
                var str = context.Reader.ReadString();
                if (string.IsNullOrWhiteSpace(str))
                    return DateTime.MinValue;
                if (DateTime.TryParse(str, out var parsed))
                    return parsed.ToUniversalTime();
                return DateTime.MinValue;
            case BsonType.Null:
                context.Reader.ReadNull();
                return DateTime.MinValue;
            default:
                context.Reader.SkipValue();
                return DateTime.MinValue;
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
    {
        context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(value));
    }
}

public sealed class FlexibleNullableDateTimeSerializer : SerializerBase<DateTime?>
{
    public override DateTime? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.CurrentBsonType;
        switch (type)
        {
            case BsonType.DateTime:
                return BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(context.Reader.ReadDateTime());
            case BsonType.String:
                var str = context.Reader.ReadString();
                if (string.IsNullOrWhiteSpace(str))
                    return null;
                if (DateTime.TryParse(str, out var parsed))
                    return parsed.ToUniversalTime();
                return null;
            case BsonType.Null:
                context.Reader.ReadNull();
                return null;
            default:
                context.Reader.SkipValue();
                return null;
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime? value)
    {
        if (value.HasValue)
        {
            context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(value.Value));
        }
        else
        {
            context.Writer.WriteNull();
        }
    }
}

public sealed class FlexibleStringOrObjectIdSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.CurrentBsonType;
        switch (type)
        {
            case BsonType.ObjectId:
                return context.Reader.ReadObjectId().ToString();
            case BsonType.String:
                return context.Reader.ReadString();
            case BsonType.Symbol:
                return context.Reader.ReadSymbol();
            case BsonType.Int32:
                return context.Reader.ReadInt32().ToString();
            case BsonType.Int64:
                return context.Reader.ReadInt64().ToString();
            case BsonType.Null:
                context.Reader.ReadNull();
                return string.Empty;
            default:
                context.Reader.SkipValue();
                return string.Empty;
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            context.Writer.WriteString(string.Empty);
        }
        else if (ObjectId.TryParse(value, out var objectId))
        {
            context.Writer.WriteObjectId(objectId);
        }
        else
        {
            context.Writer.WriteString(value);
        }
    }
}
