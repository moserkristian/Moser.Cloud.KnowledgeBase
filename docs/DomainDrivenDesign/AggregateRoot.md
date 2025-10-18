In Domain Driven Design the Aggregate Root is assigned to Entity 
that is considered as logical cluster needing to preserve integrity,
consistency and business rules.

That is done by having a IAggregateRoot interface assigned to Entity,
while Repository has constraint to work only with Aggregate Roots.

All business rules are in Domain, while Application layer calls methods
of Aggregate Root so business rules are not bypassed.

This also helps to prevent too many round trips to database
or accessing data from multiple threads not locking the Aggregate Root.

It may be enough for determining Aggregate Root by having collection of child entities.

Child entities must have internal constructor.

Child entities can not have any navigation properties to Aggregate Root,
they do not know who is their parent.

IAggregateRoot with eager load prevents extra queries to database,
prevents mistakes,
prevents broken invariants like total price of Order not matching sum of OrderItems.

In actual implementation, I choose style of naming folder Domain/<EntityName>s,
which contains Entity marked as IAggregateRoot and any child entities, events,
value objects, repositories and other related classes.