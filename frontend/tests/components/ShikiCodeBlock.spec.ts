import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ShikiCodeBlock from '~/components/common/ShikiCodeBlock.vue'

describe('ShikiCodeBlock.vue', () => {
  it('correctly detects C# for CQRS / LINQ queries with EF Core and Select/DbContext', () => {
    const csharpCode = `// ✅ SENIOR PATTERN: CQRS / Feature-based Query sử dụng trực tiếp DbContext với Projection
public record GetUsersQuery : IRequest<List<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly AppDbContext _context;
    public GetUsersHandler(AppDbContext context) => _context = context;

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking() // 1. Tối ưu bộ nhớ, bỏ qua Change Tracker
            .Select(u => new UserDto(u.Id, u.Name, u.Email)) // 2. Projection: Chỉ SELECT
            .ToListAsync(cancellationToken);
    }
}`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: csharpCode,
        category: 1
      }
    })

    expect(wrapper.text()).toContain('C# / .NET 10')
  })

  it('correctly detects C# for CQRS query even when category is not passed or tags contain database topic', () => {
    const csharpCode = `public record GetUsersQuery : IRequest<List<UserDto>>;
public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>> {
    public async Task<List<UserDto>> Handle(GetUsersQuery req, CancellationToken ct) {
        return await _context.Users.AsNoTracking().ToListAsync(ct);
    }
}`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: csharpCode,
        tags: ['performance', 'architecture']
      }
    })

    expect(wrapper.text()).toContain('C# / .NET 10')
  })

  it('correctly detects PostgreSQL / SQL for SQL DDL and DML statements', () => {
    const sqlCode = `-- ✅ SENIOR PATTERN: Leave 15% page headroom for in-page HOT updates
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY,
    last_seen_at TIMESTAMPTZ,
    ip_address INET
) WITH (fillfactor = 85);

SELECT n_tup_upd, n_tup_hot_upd FROM pg_stat_user_tables WHERE relname = 'user_sessions';`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: sqlCode,
        category: 2,
        tags: ['postgres17', 'mvcc', 'hot-updates']
      }
    })

    expect(wrapper.text()).toContain('PostgreSQL / SQL')
  })

  it('correctly detects TypeScript / Vue 3 for reactive state snippet', () => {
    const vueCode = `const telemetryData = shallowRef<TelemetryItem[]>([]);

socket.on('telemetry', (batch) => {
  telemetryData.value = Object.freeze(batch);
});`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: vueCode,
        category: 0
      }
    })

    expect(wrapper.text()).toContain('TypeScript')
  })

  it('correctly detects Rust for Rust syntax', () => {
    const rustCode = `pub struct CacheEntry {
    pub key: String,
    pub value: Vec<u8>,
}
impl CacheEntry {
    pub fn new(key: &str) -> Self {
        println!("creating new entry");
    }
}`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: rustCode
      }
    })

    expect(wrapper.text()).toContain('Rust')
  })

  it('correctly detects Go for Go syntax', () => {
    const goCode = `package main
import "fmt"
func processQueue(ch chan int) {
    defer close(ch)
}`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: goCode
      }
    })

    expect(wrapper.text()).toContain('Go')
  })

  it('correctly detects Python for Python syntax', () => {
    const pythonCode = `class DataProcessor:
    def __init__(self, name: str):
        self.name = name

    async def run(self):
        print(f"Running {self.name}")`

    const wrapper = mount(ShikiCodeBlock, {
      props: {
        code: pythonCode
      }
    })

    expect(wrapper.text()).toContain('Python')
  })
})
