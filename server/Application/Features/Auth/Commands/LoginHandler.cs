using AutoMapper;
using MediatR;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Features.Auth.Commands;

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _pwd;
    private readonly IMapper _mapper;

    public LoginHandler(IUnitOfWork uow, IJwtService jwt, IPasswordService pwd, IMapper mapper)
    {
        _uow = uow; _jwt = jwt; _pwd = pwd; _mapper = mapper;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var users = await _uow.Repository<User>().FindAsync(u => u.Email == request.Email);
        var user = users.FirstOrDefault() ?? throw new Exception("Invalid credentials");

        if (!_pwd.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        return new AuthResponse(_jwt.GenerateToken(user), string.Empty, _mapper.Map<UserDto>(user));
    }
}
