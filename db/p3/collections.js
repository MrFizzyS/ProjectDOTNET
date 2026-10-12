// Run with mongosh. Validators intentionally allow additional properties for evolving content.
const dbName = 'rpg_content';
const target = db.getSiblingDB(dbName);
const definitions = {
  PlayerProfile: { $jsonSchema: { bsonType: 'object', required: ['_id', 'handle', 'preferences', 'characters', 'updatedAt'], properties: { _id: { bsonType: 'string' }, handle: { bsonType: 'string' }, customerId: { bsonType: ['string', 'null'] }, preferences: { bsonType: 'object' }, characters: { bsonType: 'array' }, updatedAt: { bsonType: 'date' } } } },
  Campaign: { $jsonSchema: { bsonType: 'object', required: ['_id', 'name', 'gameSystemId', 'gameSystemName', 'sessions', 'members'], properties: { _id: { bsonType: 'string' }, name: { bsonType: 'string' }, gameSystemId: { bsonType: 'string' }, gameSystemName: { bsonType: 'string' }, sessions: { bsonType: 'array' }, members: { bsonType: 'array' } } } },
  Review: { $jsonSchema: { bsonType: 'object', required: ['_id', 'productId', 'authorCustomerId', 'rating', 'createdAt'], properties: { _id: { bsonType: 'string' }, productId: { bsonType: 'string' }, authorCustomerId: { bsonType: 'string' }, rating: { bsonType: ['int', 'long', 'double'], minimum: 1, maximum: 5 }, createdAt: { bsonType: 'date' } } } },
  ContentArticle: { $jsonSchema: { bsonType: 'object', required: ['_id', 'slug', 'title', 'blocks', 'publishedAt'], properties: { _id: { bsonType: 'string' }, slug: { bsonType: 'string' }, title: { bsonType: 'string' }, blocks: { bsonType: 'array' }, publishedAt: { bsonType: ['date', 'null'] } } } }
};
for (const [name, validator] of Object.entries(definitions)) {
  if (!target.getCollectionNames().includes(name)) target.createCollection(name, { validator });
  else target.runCommand({ collMod: name, validator, validationLevel: 'moderate', validationAction: 'error' });
}
target.PlayerProfile.createIndex({ handle: 1 }, { unique: true, name: 'UX_PlayerProfile_handle' });
target.Campaign.createIndex({ gameSystemId: 1, updatedAt: -1 }, { name: 'IX_Campaign_system_updated' });
target.Review.createIndex({ productId: 1, createdAt: -1 }, { name: 'IX_Review_product_recent' });
target.Review.createIndex({ authorCustomerId: 1, productId: 1 }, { unique: true, name: 'UX_Review_author_product' });
target.ContentArticle.createIndex({ slug: 1 }, { unique: true, name: 'UX_ContentArticle_slug' });
print(`Collections and indexes ready in ${dbName}.`);
