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
