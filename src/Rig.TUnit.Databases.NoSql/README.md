# Rig.TUnit.Databases.NoSql

NoSQL / document-store base layer. Ships `INoSqlRig`, `DocumentFixtureBase`, `NoSqlRigBuilder<TSelf>`, `JsonDocumentAssert` (system-field-scrubbing deep equality), and `ChangeFeedCapture`. Concrete providers: `.Redis`, `.Cosmos`, `.Mongo`, `.Dynamo`, `.Cassandra`, `.EventStore`, `.ElasticSearch`.

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.Redis  # etc.
```

## Dependencies

`Rig.TUnit.Databases`
