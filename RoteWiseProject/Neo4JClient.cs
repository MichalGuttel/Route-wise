//using Neo4j.Driver;
//using Neo4JSample.Settings;
//using System;
//using System.Threading.Tasks;
//using System.Xml.Linq;

//namespace Neo4JSample
//{
//    public class Neo4JClient : IDisposable
//    {
//        private readonly IDriver _driver;

//        public Neo4JClient(IConnectionSettings settings)
//        {
//            _driver = GraphDatabase.Driver(settings.Uri, settings.AuthToken);
//        }

//        public async Task BuildGraphAsync()
//        {
//            IAsyncSession session = _driver.AsyncSession();
//            try
//            {
//                // Start a transaction
//                IAsyncTransaction tx = await session.BeginTransactionAsync();

//                // Create source node
//                await CreateNode(tx, "source", "source");

//                // Create flight nodes and relationships
//                var flightNodes = new List<string> { "flight1", "flight2", "flight3" };
//                foreach (var flightNode in flightNodes)
//                {
//                    await CreateNode(tx, flightNode, "flight");
//                    await CreateRelationship(tx, "source", flightNode, "source_to_flight");
//                }

//                // Create hotel nodes and relationships
//                var hotelNodes = new List<string> { "hotel1", "hotel2", "hotel3" };
//                foreach (var hotelNode in hotelNodes)
//                {
//                    await CreateNode(tx, hotelNode, "hotel");
//                    foreach (var flightNode in flightNodes)
//                    {
//                        await CreateRelationship(tx, flightNode, hotelNode, "flight_to_hotel");
//                    }
//                }


//                // Create attraction nodes and relationships
//                var attractionNodes = new List<string> { "attraction1", "attraction2", "attraction3" };
//                foreach (var attractionNode in attractionNodes)
//                {
//                    await CreateNode(tx, attractionNode, "attraction");
//                    foreach (var hotelNode in hotelNodes)
//                    {
//                        await CreateRelationship(tx, hotelNode, attractionNode, "hotel_to_attraction");
//                    }
//                }

//                // Repeat this process for flight to hotel, hotel to attraction, attraction to attraction, etc.

//                // Commit the transaction
//                await tx.CommitAsync();

//            }
//            finally
//            {
//                await session.CloseAsync();
//            }
//        }

//        private async Task CreateNode(IAsyncTransaction tx, string nodeId, string nodeType)
//        {
//            await tx.RunAsync("CREATE (n:Node { Id: $id, Type: $type })", new { id = nodeId, type = nodeType });
//        }

//       // private async Task CreateRelationship(IAsyncTransaction tx, string sourceNodeId, string targetNodeId, string relationshipType)
//        private async Task CreateRelationship(IAsyncTransaction tx, string sourceNodeId, string targetNodeId, string relationship)
//        {
//            await tx.RunAsync("MATCH (source:Node { Id: $sourceId }), (target:Node { Id: $targetId }) " +
//                          //   D    "CREATE (source)-[:{relationshipType}]->(target)", new { sourceId = sourceNodeId, targetId = targetNodeId });
//                          "CREATE (source)-[:RELATIONSHIP_TYPE {relationshipType: $relationshipType}]->(target)", new { sourceId = sourceNodeId, targetId = targetNodeId ,relationshipType=relationship });
//        }



//        public void Dispose()
//        {
//            _driver?.Dispose();
//        }
//    }
//}



////// Copyright (c) Philipp Wagner. All rights reserved.
////// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////using Neo4j.Driver;
////using Neo4JSample.Model;
////using System;
////using System.Collections.Generic;
////using System.Text;
////using System.Threading.Tasks;
////using Neo4JSample.Serializer;
////using Neo4JSample.Settings;

////namespace Neo4JSample
////{
////    public class Neo4JClient : IDisposable
////    {
////        private readonly IDriver driver;

////        public Neo4JClient(IConnectionSettings settings)
////        {
////            this.driver = GraphDatabase.Driver(settings.Uri, settings.AuthToken);
////        }

////        public async Task CreateIndices()
////        {
////            string[] queries = {
////                "CREATE INDEX ON :Movie(title)",
////                "CREATE INDEX ON :Movie(id)",
////                "CREATE INDEX ON :Person(id)",
////                "CREATE INDEX ON :Person(name)",
////                "CREATE INDEX ON :Genre(name)"
////            };

////            using (var session = driver.AsyncSession())
////            {
////                foreach(var query in queries)
////                {
////                    await session.RunAsync(query);
////                }
////            }
////        }

////        public async Task CreatePersons(IList<Person> persons)
////        {
////            string cypher = new StringBuilder()
////                .AppendLine("UNWIND {persons} AS person")
////                .AppendLine("MERGE (p:Person {name: person.name})")
////                .AppendLine("SET p = person")
////                .ToString();

////            using (var session = driver.AsyncSession())
////            {
////                await session.RunAsync(cypher, new Dictionary<string, object>() { { "persons", ParameterSerializer.ToDictionary(persons) } });
////            }
////        }

////        public async Task CreateGenres(IList<Genre> genres)
////        {
////            string cypher = new StringBuilder()
////                .AppendLine("UNWIND {genres} AS genre")
////                .AppendLine("MERGE (g:Genre {name: genre.name})")
////                .AppendLine("SET g = genre")
////                .ToString();

////            using (var session = driver.AsyncSession())
////            {
////                await session.RunAsync(cypher, new Dictionary<string, object>() { { "genres", ParameterSerializer.ToDictionary(genres) } });
////            }
////        }

////        public async Task CreateMovies(IList<Movie> movies)
////        {
////            string cypher = new StringBuilder()
////                .AppendLine("UNWIND {movies} AS movie")
////                .AppendLine("MERGE (m:Movie {id: movie.id})")
////                .AppendLine("SET m = movie")
////                .ToString();

////            using (var session = driver.AsyncSession())
////            {
////                await session.RunAsync(cypher, new Dictionary<string, object>() { { "movies", ParameterSerializer.ToDictionary(movies) } });
////            }
////        }

////        public async Task CreateRelationships(IList<MovieInformation> metadatas)
////        {
////            string cypher = new StringBuilder()
////                .AppendLine("UNWIND {metadatas} AS metadata")
////                // Find the Movie:
////                 .AppendLine("MATCH (m:Movie { title: metadata.movie.title })")
////                 // Create Cast Relationships:
////                 .AppendLine("UNWIND metadata.cast AS actor")   
////                 .AppendLine("MATCH (a:Person { name: actor.name })")
////                 .AppendLine("MERGE (a)-[r:ACTED_IN]->(m)")
////                  // Create Director Relationship:
////                 .AppendLine("WITH metadata, m")
////                 .AppendLine("MATCH (d:Person { name: metadata.director.name })")
////                 .AppendLine("MERGE (d)-[r:DIRECTED]->(m)")
////                // Add Genres:
////                .AppendLine("WITH metadata, m")
////                .AppendLine("UNWIND metadata.genres AS genre")
////                .AppendLine("MATCH (g:Genre { name: genre.name})")
////                .AppendLine("MERGE (m)-[r:GENRE]->(g)")
////                .ToString();


////            using (var session = driver.AsyncSession())
////            {
////                await session.RunAsync(cypher, new Dictionary<string, object>() { { "metadatas", ParameterSerializer.ToDictionary(metadatas) } });
////            }
////        }

////        public void Dispose()
////        {
////            driver?.Dispose();
////        }
////    }
////}public class DijkstraAlgorithm
///

//{
//    public Dictionary<Node, int> Distances { get; private set; }
//public Dictionary<Node, Node> PreviousNodes { get; private set; }
//private readonly IGraph _graph;

//public DijkstraAlgorithm(IGraph graph)
//{
//    _graph = graph;
//}

//public void Execute(Node source)
//{
//    Distances = new Dictionary<Node, int>();
//    PreviousNodes = new Dictionary<Node, Node>();
//    var priorityQueue = new PriorityQueue<Node, int>();

//    foreach (var node in _graph.Nodes)
//    {
//        if (node == source)
//            Distances[node] = 0;
//        else
//            Distances[node] = int.MaxValue;

//        priorityQueue.Enqueue(node, Distances[node]);
//    }

//    while (priorityQueue.Count > 0)
//    {
//        var currentNode = priorityQueue.Dequeue();
//        foreach (var neighbor in _graph.GetNeighbors(currentNode))
//        {
//            var alt = Distances[currentNode] + _graph.GetDistance(currentNode, neighbor);
//            if (alt < Distances[neighbor])
//            {
//                Distances[neighbor] = alt;
//                PreviousNodes[neighbor] = currentNode;
//                priorityQueue.UpdatePriority(neighbor, alt);
//            }
//        }
//    }
//}

//public List<Node> GetPath(Node target)
//{
//    var path = new List<Node>();
//    for (var node = target; node != null; node = PreviousNodes.ContainsKey(node) ? PreviousNodes[node] : null)
//        path.Add(node);

//    path.Reverse();
//    return path;
//}
//}
//```
