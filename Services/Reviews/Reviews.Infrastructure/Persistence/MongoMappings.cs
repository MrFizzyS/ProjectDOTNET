using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Application.Common.Models;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Sync)
        {
            if (_registered)
            {
                return;
            }

            ConventionRegistry.Register(
                "ReviewsCamelCase",
                new ConventionPack { new CamelCaseElementNameConvention() },
                _ => true);

            BsonClassMap.RegisterClassMap<BaseEntity>(classMap => classMap.AutoMap());
            BsonClassMap.RegisterClassMap<Author>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapConstructor(
                    typeof(Author).GetConstructor([typeof(string), typeof(string)])!,
                    nameof(Author.CustomerId),
                    nameof(Author.Handle));
            });
            BsonClassMap.RegisterClassMap<ReviewAspects>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapConstructor(
                    typeof(ReviewAspects).GetConstructor([typeof(int), typeof(int), typeof(int)])!,
                    nameof(ReviewAspects.Usability),
                    nameof(ReviewAspects.Story),
                    nameof(ReviewAspects.Value));
            });
            BsonSerializer.RegisterSerializer(new ReviewRatingSerializer());
            BsonClassMap.RegisterClassMap<ReviewAspectsDto>(classMap => classMap.AutoMap());
            BsonClassMap.RegisterClassMap<ReviewReplyDto>(classMap => classMap.AutoMap());
            BsonClassMap.RegisterClassMap<ReviewDto>(classMap => classMap.AutoMap());
            BsonClassMap.RegisterClassMap<ReviewReply>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapConstructor(
                    typeof(ReviewReply).GetConstructor([typeof(Author), typeof(string), typeof(DateTime?)])!,
                    nameof(ReviewReply.Author),
                    nameof(ReviewReply.Body),
                    nameof(ReviewReply.CreatedAt));
            });
            BsonClassMap.RegisterClassMap<Review>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapMember(review => review.Replies).SetElementName("replies");
            });

            _registered = true;
        }
    }

    private sealed class ReviewRatingSerializer : SerializerBase<ReviewRating>
    {
        public override ReviewRating Deserialize(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var value = reader.GetCurrentBsonType() switch
            {
                BsonType.Int32 => reader.ReadInt32(),
                BsonType.Int64 => checked((int)reader.ReadInt64()),
                BsonType.Double => ReadInteger(reader.ReadDouble()),
                _ => throw new BsonSerializationException("Review rating must be a BSON number.")
            };

            return new ReviewRating(value);
        }

        private static int ReadInteger(double value)
        {
            if (!double.IsFinite(value)
                || value != Math.Truncate(value)
                || value < int.MinValue
                || value > int.MaxValue)
            {
                throw new BsonSerializationException("Review rating must be an integer.");
            }

            return (int)value;
        }

        public override void Serialize(
            BsonSerializationContext context,
            BsonSerializationArgs args,
            ReviewRating value)
            => context.Writer.WriteInt32(value.Value);
    }
}
