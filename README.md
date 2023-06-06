# Responses

![.NET Core](https://github.com/slipalison/Responses/workflows/.NET%20Core/badge.svg?event=push)
![.NET Core](https://github.com/slipalison/Responses/workflows/.NET%20Core/badge.svg)
[![codecov](https://codecov.io/gh/slipalison/Responses/branch/master/graph/badge.svg)](https://codecov.io/gh/slipalison/Responses)

### Features
> Mais um Notification pattern 

----

## Responses　

```CSharp
    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var entitie =
            await _context.ToDos.FindAsync(new object?[] { id },
                cancellationToken: cancellationToken);

        if (entitie == null) return Result.Fail("404", "Tarefa não encontrada");

        _context.ToDos.Remove(entitie);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<ToDoItemEntity>> Update(ToDoItemEntity entity, CancellationToken cancellationToken = default)
    {
        var entitie =
            await _context.ToDos.FindAsync(new object?[] { entity.Id },
                cancellationToken: cancellationToken);

        if (entitie == null) return Result.Fail<ToDoItemEntity>("404", "Tarefa não encontrada");

        entitie.Deadline = entity.Deadline;
        entitie.Name = entity.Name;
        entitie.Status = entity.Status;
        _context.ToDos.Update(entitie);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok(entitie);
    }
```
## Responses.Http

É uma extenção do [Flurl](https://flurl.dev/) que faz o parse para o Response

```CSharp

    [Fact]
    public async Task AddAndGetTodos()
    {
        var t = await CallHttp("/Todo").PostJsonAsync(new ToDoItemCreateCommand
            { Deadline = DateTime.Now.AddDays(2), Name = "Comprar ovos" }).ReceiveResult<ToDoItemEntity>();

        var tResult = await CallHttp("/Todo").GetAsync().ReceiveResult<List<ToDoItemEntity>>();

        Assert.True(t.IsSuccess);
        Assert.True(tResult.IsSuccess);
        Assert.NotEmpty(t.Value.Name!);
        Assert.NotEmpty(tResult.Value);

        Assert.Contains(t.Value.Id, tResult.Value.Select(x => x.Id));
    }

    [Fact]
    public async Task AddTaskWithError()
    {
        var t = await CallHttp("/Todo").PostJsonAsync(new ToDoItemCreateCommand
            { Deadline = DateTime.Now.AddDays(-2), Name = "Co" }).ReceiveResult<ToDoItemEntity>();

        Assert.False(t.IsSuccess);
        Assert.True(t.Error.Code == "404");
        Assert.Contains("DeadLine", t.Error.Errors.Select(x => x.Key));
        Assert.Contains("Name", t.Error.Errors.Select(x => x.Key));
    }
```