using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Services;

namespace PlanningPoker.Controller.Mappers;

public class RoomExistsResponseMapper(IRoomNameParser nameParser, IRoomNameWordResponseMapper wordMapper) : IRoomExistsResponseMapper
{
    public RoomExistsResponse Map(string roomId)
    {
        return new RoomExistsResponse
        {
            Id = roomId,
            Exists = true,
            NameWords = nameParser.ParseWords(roomId)?.Select(wordMapper.Map).ToList()
        };
    }
}
