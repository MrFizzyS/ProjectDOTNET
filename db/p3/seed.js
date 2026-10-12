const target = db.getSiblingDB('rpg_content');
const now = new Date('2026-01-15T12:00:00Z');

// Stable string IDs make reruns deterministic and allow references in other contexts.
target.PlayerProfile.replaceOne({ _id: 'player-ada' }, {
  _id: 'player-ada', handle: 'ada_rolls', customerId: '11111111-1111-4111-8111-111111111111',
  preferences: { locale: 'uk-UA', diceTheme: 'amethyst', notifications: { sessionReminder: true, reviewReplies: true } },
  characters: [
    { characterId: 'char-lyra', name: 'Lyra Emberfall', class: 'Bard', level: 4, system: '5e', traits: ['curious', 'brave'] },
    { characterId: 'char-bram', name: 'Bram Underbough', class: 'Ranger', level: 2, system: '5e', companion: { name: 'Moss', species: 'owl' } }
  ], updatedAt: now
}, { upsert: true });

target.Campaign.replaceOne({ _id: 'campaign-ember-crown' }, {
  _id: 'campaign-ember-crown', name: 'The Ember Crown', gameSystemId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', gameSystemName: 'Dungeons & Dragons 5e',
  organizerCustomerId: '11111111-1111-4111-8111-111111111111', productIds: ['22222222-2222-4222-8222-222222222222'], status: 'active',
  members: [{ profileId: 'player-ada', role: 'game-master', joinedAt: now }, { profileId: 'player-ivo', role: 'player', joinedAt: now }],
  sessions: [{ number: 1, title: 'Ashes at Dawn', startsAt: new Date('2026-02-10T18:00:00Z'), recap: 'The party met at the old watchtower.', attendance: ['player-ada', 'player-ivo'] }],
  safetyTools: { lines: ['harm to children'], veils: ['romance'], openDoor: true }, createdAt: now, updatedAt: now
}, { upsert: true });

target.Review.replaceOne({ _id: 'review-ember-ada' }, {
  _id: 'review-ember-ada', productId: '22222222-2222-4222-8222-222222222222', productName: 'The Ember Crown: Adventure',
  authorCustomerId: '11111111-1111-4111-8111-111111111111', authorHandle: 'ada_rolls', orderId: '33333333-3333-4333-8333-333333333333', purchaseStatusSnapshot: 'Completed',
  rating: 5, aspects: { usability: 5, story: 5, value: 4 }, body: 'A vivid adventure with memorable encounters.', replies: [], createdAt: now,
  survey: { tableSize: 4, campaignLength: 'short', playedOnline: false }
}, { upsert: true });

target.ContentArticle.replaceOne({ _id: 'article-session-zero' }, {
  _id: 'article-session-zero', slug: 'session-zero-checklist', title: 'Session Zero Checklist',
  blocks: [{ type: 'paragraph', text: 'Agree on tone, schedule, and table boundaries.' }, { type: 'checklist', items: ['Set expectations', 'Choose safety tools', 'Plan communication'] }],
  tags: ['game-mastering', 'safety'], authorProfileId: 'player-ada', publishedAt: now, revision: 1
}, { upsert: true });

print('MongoDB seed documents upserted.');
