# Technical Design: Knowledge Graph Pillar Constellations & Orphan Node Resolution

## Context & Architecture Overview

TechDaily renders an interactive 2D architecture knowledge graph at `/graph` using Cytoscape.js. In production exploration, two primary defects compromised graph quality:
1. A blind Cartesian product in `GetKnowledgeGraphQueryHandler.cs` connected books to every topic in their category (`book.Category == topic.Category`). When web-crawled books (such as `aspnet-core-aspnetcore-10.0`) defaulted to `0` (`FrontendWeb`), the .NET book was linked to all Days 1–7 Vue 3 topics.
2. Pillars lacking books or user notes (e.g. `DatabaseStorage` and `SystemDesign`) produced topics with degree 0. Cytoscape's CoSE algorithm, experiencing pure repulsive forces without spring edges, repelled them into an overlapping horizontal line along the bottom border.

This design introduces **Pillar Hub Nodes** as permanent gravitational centers, connects every topic and book via `TopicToPillar` and `BookToPillar` edges, refines book-to-topic relational matching, adds smart book category auto-inference, and tunes CoSE physics parameters.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       PILLAR CONSTELLATION TOPOLOGY                         │
│                                                                             │
│               [Topic: Vue Reactivity]                                       │
│                         \                                                   │
│            [Topic: DOM] ─ [PILLAR: Frontend & Web] ── [Book: Vue 3 Guide]   │
│                         /                                                   │
│               [Topic: SSR]                                                  │
│                                                                             │
│    ════════════════════════════════════════════════════════════════════     │
│                                                                             │
│               [Topic: Generational GC]                                      │
│                         \                                                   │
│        [Topic: Channels] ─ [PILLAR: Backend (.NET)] ── [Book: CLR via C#]   │
│                         /         │                                         │
│               [Topic: Span<T>]    │ (Specific Content Match)                │
│                                   ▼                                         │
│                       [Topic: Async State Machine]                          │
│                                                                             │
│    ════════════════════════════════════════════════════════════════════     │
│                                                                             │
│               [Topic: MVCC Engine]                                          │
│                         \                                                   │
│          [Topic: WAL] ─── [PILLAR: Database & Storage]                      │
│                         /                                                   │
│               [Topic: B-Tree Indexes] (Degree >= 1 guaranteed! No orphans) │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Backend DTO & Contract Updates

### DTO Constants (`KnowledgeGraphDtos.cs`)
```csharp
public static class GraphNodeType
{
    public const string Pillar = "pillar";
    public const string Topic = "topic";
    public const string Book = "book";
    public const string Card = "card";
    public const string Highlight = "highlight";
}

public static class GraphRelationType
{
    public const string TopicToPillar = "TopicToPillar";
    public const string BookToPillar = "BookToPillar";
    public const string CardToTopic = "CardToTopic";
    public const string BookToTopic = "BookToTopic";
    public const string HighlightToBook = "HighlightToBook";
    public const string HighlightToTopic = "HighlightToTopic";
    public const string SharedTag = "SharedTag";
}
```

### Pillar Definitions
In `GetKnowledgeGraphQueryHandler.cs`, define the 5 canonical architectural pillars:
```csharp
private static readonly (Category Category, string Id, string Label, string Subtitle)[] CanonicalPillars =
[
    (Category.FrontendWeb, "pillar-FrontendWeb", "Frontend & Web", "Browser Internals, DOM, Reactivity & Protocols"),
    (Category.BackendDotNet, "pillar-BackendDotNet", "Backend (.NET)", ".NET Runtime, CLR, Concurrency & Async I/O"),
    (Category.DatabaseStorage, "pillar-DatabaseStorage", "Database & Storage", "PostgreSQL 17, MVCC, Indexing & Storage Engines"),
    (Category.SystemDesign, "pillar-SystemDesign", "Distributed Systems", "Architecture, Microservices, Caching & Resilience"),
    (Category.EngineeringCraft, "pillar-EngineeringCraft", "Engineering Craft", "Code Quality, Mindset, Leadership & Productivity")
];
```

---

## 2. Graph Derivation Logic (`GetKnowledgeGraphQueryHandler.cs`)

### 2.1 Generating Pillar Nodes
Each canonical pillar is added to `nodes` as a `GraphNodeType.Pillar`:
```csharp
foreach (var (cat, id, label, subtitle) in CanonicalPillars)
{
    nodes.Add(new GraphNodeDto(
        Id: id,
        Label: label,
        Type: GraphNodeType.Pillar,
        Category: cat.ToString(),
        Subtitle: subtitle,
        DayOrder: null,
        Summary: $"Core architectural pillar for {label}."
    ));
}
```

### 2.2 Generating Topic-to-Pillar Edges (`TopicToPillar`)
Every topic links directly to its parent Pillar Hub:
```csharp
foreach (var topic in topics)
{
    var pillarId = $"pillar-{topic.Category}";
    edges.Add(new GraphEdgeDto(
        Id: $"edge-topic-{topic.Id}-pillar-{topic.Category}",
        Source: topic.Id.ToString(),
        Target: pillarId,
        RelationType: GraphRelationType.TopicToPillar,
        Label: "Pillar",
        Weight: 2
    ));
}
```
*Result:* Guarantees that every curriculum topic has $degree \ge 1$.

### 2.3 Generating Book-to-Pillar Edges (`BookToPillar`)
Every published book links to its respective Pillar Hub:
```csharp
foreach (var book in books)
{
    // If book is a universal multi-pillar curriculum (e.g. 30-Day Master Curriculum)
    if (book.SourceType == SourceType.MarkdownSeries && book.TotalChunks >= 30)
    {
        foreach (var cat in new[] { Category.FrontendWeb, Category.BackendDotNet, Category.DatabaseStorage, Category.SystemDesign })
        {
            edges.Add(new GraphEdgeDto(
                Id: $"edge-book-{book.Id}-pillar-{cat}",
                Source: book.Id.ToString(),
                Target: $"pillar-{cat}",
                RelationType: GraphRelationType.BookToPillar,
                Label: "Curriculum",
                Weight: 2
            ));
        }
    }
    else
    {
        edges.Add(new GraphEdgeDto(
            Id: $"edge-book-{book.Id}-pillar-{book.Category}",
            Source: book.Id.ToString(),
            Target: $"pillar-{book.Category}",
            RelationType: GraphRelationType.BookToPillar,
            Label: "Library",
            Weight: 2
        ));
    }
}
```

### 2.4 Eliminating Cartesian Product for `BookToTopic`
Instead of linking every book to all topics in its category, books connect to topics only if there is a genuine textual or structural match:
1. Match topic titles or slugs against book titles or book chunk chapter titles.
2. Match if the book chunk directly links to the topic.
If no specific match exists, the book remains cleanly linked to the Pillar Hub without creating dozens of spurious topic edges.

---

## 3. Intelligent Book Category Inference in Crawler

### Detection Rules
In `WebArticleCrawler.cs` and frontend `library.vue`:
```csharp
public static Category InferCategoryFromContext(string title, string url, string? content = null)
{
    var combined = $"{title} {url} {content}".ToLowerInvariant();

    if (combined.Contains("aspnet") || combined.Contains("aspnetcore") ||
        combined.Contains("dotnet") || combined.Contains(".net") ||
        combined.Contains("csharp") || combined.Contains("c#") ||
        combined.Contains("entityframework") || combined.Contains("efcore"))
    {
        return Category.BackendDotNet;
    }

    if (combined.Contains("postgres") || combined.Contains("postgresql") ||
        combined.Contains("redis") || combined.Contains("mysql") ||
        combined.Contains("mongodb") || combined.Contains("database") ||
        combined.Contains("sql") || combined.Contains("storage engine"))
    {
        return Category.DatabaseStorage;
    }

    if (combined.Contains("system design") || combined.Contains("distributed") ||
        combined.Contains("microservice") || combined.Contains("kafka") ||
        combined.Contains("kubernetes") || combined.Contains("docker") ||
        combined.Contains("outbox") || combined.Contains("event sourcing"))
    {
        return Category.SystemDesign;
    }

    if (combined.Contains("atomic habits") || combined.Contains("deep work") ||
        combined.Contains("pragmatic") || combined.Contains("mindset") ||
        combined.Contains("productivity") || combined.Contains("leadership") ||
        combined.Contains("soft skills"))
    {
        return Category.EngineeringCraft;
    }

    if (combined.Contains("vue") || combined.Contains("react") ||
        combined.Contains("angular") || combined.Contains("frontend") ||
        combined.Contains("browser") || combined.Contains("css") ||
        combined.Contains("html") || combined.Contains("dom") ||
        combined.Contains("javascript") || combined.Contains("typescript"))
    {
        return Category.FrontendWeb;
    }

    return Category.BackendDotNet; // Safe default for engineering platforms instead of FrontendWeb
}
```

In `frontend/pages/library.vue`:
- Upon crawling an article, automatically set `importCategory` according to the inferred category based on the URL and returned title, preventing accidental `0` (`FrontendWeb`) misclassification.

---

## 4. Frontend Cytoscape Canvas & Physics Tuning

### 4.1 CoSE Layout Parameters (`GraphCanvas.vue`)
```typescript
const coseOptions: CoseLayoutOptions = {
  name: 'cose',
  animate: true,
  animationDuration: 1000,
  coolingFactor: 0.95,
  numIter: 300,
  randomize: true,            // Prevents initial stacking
  componentSpacing: 120,      // Guarantees generous spacing between pillar clusters
  fit: true,
  padding: 60,
  nodeRepulsion: () => 500000,// Stronger repulsion prevents local overlaps
  idealEdgeLength: () => 120, // Increased spring length balances cluster spread
  edgeElasticity: () => 100,
  gravity: 80,
  stop: () => {
    // Freezes completely after settling, 0% CPU
  }
}
```

### 4.2 Pillar Node Cytoscape Stylesheet
```typescript
{
  selector: 'node[type = "pillar"]',
  style: {
    'shape': 'ellipse',
    'width': 56,
    'height': 56,
    'font-size': '13px',
    'font-weight': 'bold',
    'border-width': 3,
    'border-color': borderClr,
    'z-index': 10
  }
},
{
  selector: 'node[type = "pillar"][category = "BackendDotNet"], node[type = "pillar"][category = "DotNet"]',
  style: {
    'background-color': '#0284c7',
    'border-color': '#38bdf8'
  }
},
{
  selector: 'node[type = "pillar"][category = "DatabaseStorage"], node[type = "pillar"][category = "Postgres"]',
  style: {
    'background-color': '#059669',
    'border-color': '#34d399'
  }
},
{
  selector: 'node[type = "pillar"][category = "SystemDesign"], node[type = "pillar"][category = "DistributedSystems"]',
  style: {
    'background-color': '#7c3aed',
    'border-color': '#a78bfa'
  }
},
{
  selector: 'node[type = "pillar"][category = "FrontendWeb"], node[type = "pillar"][category = "Frontend"]',
  style: {
    'background-color': '#d97706',
    'border-color': '#fbbf24'
  }
},
{
  selector: 'node[type = "pillar"][category = "EngineeringCraft"], node[type = "pillar"][category = "Craft"]',
  style: {
    'background-color': '#e11d48',
    'border-color': '#fb7185'
  }
}
```

### 4.3 Node Detail Drawer (`GraphDetailDrawer.vue`)
When `nodeType === 'pillar'`:
- Render an architectural emblem icon (e.g. `Layers`, `Landmark`, or `Cpu`).
- Display the pillar description and a count of connected topics, books, and flashcards.
- Provide a quick CTA to filter the entire canvas to this pillar (`store.selectedCategory = node.category`).

---

## 5. Automated Verification Plan

1. **Backend Query Tests (`GetKnowledgeGraphQueryHandlerTests.cs`):**
   - Verify `Nodes` contains exactly 5 nodes with `Type == GraphNodeType.Pillar`.
   - Verify every topic in the database has a corresponding `TopicToPillar` edge.
   - Verify that no topics have zero edges ($degree \ge 1$).
   - Verify multi-disciplinary books connect to all 4 technical pillars via `BookToPillar`.
   - Verify specific book-to-topic matching without Cartesian category product.
2. **Category Inference Tests:**
   - Verify URLs containing `aspnet-core`, `csharp`, and `dotnet` infer `Category.BackendDotNet`.
3. **Frontend Store & UI Integration:**
   - Verify `filteredNodes` and `filteredEdges` handle `type: "pillar"`.
   - Verify detail drawer opens with pillar metadata when a pillar node is clicked.
