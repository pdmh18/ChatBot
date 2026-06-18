using MediatR;
using Application.Common.DTOs;

namespace Application.Features.Auth.Commands;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<AuthResponse>;
