using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
