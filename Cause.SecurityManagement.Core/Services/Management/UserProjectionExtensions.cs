using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cause.SecurityManagement.Models;
using Cause.SecurityManagement.Models.DataTransferObjects.Management;

namespace Cause.SecurityManagement.Core.Services.Management
{
    public static class UserProjectionExtensions
    {
        public static Expression<Func<TUser, TDto>> WithAdditionalInformation<TUser, TDto>(
            this Expression<Func<TUser, TDto>> baseProjection,
            Expression<Func<TUser, string>> additionalInformation)
            where TUser : User
            where TDto : IHasAdditionalInformation
        {
            var userParameter = baseProjection.Parameters[0];
            var rebasedBody = new ParameterReplacer(additionalInformation.Parameters[0], userParameter)
                .Visit(additionalInformation.Body);

            if (baseProjection.Body is not MemberInitExpression memberInit)
                throw new ArgumentException("Base projection must be an object initializer.", nameof(baseProjection));
            var property = typeof(TDto).GetProperty(
                nameof(IHasAdditionalInformation.AdditionalInformation),
                BindingFlags.Public | BindingFlags.Instance);
            var binding = Expression.Bind(property!, rebasedBody);

            var updatedInit = memberInit.Update(
                memberInit.NewExpression,
                memberInit.Bindings.Append(binding));

            return Expression.Lambda<Func<TUser, TDto>>(updatedInit, userParameter);
        }

        private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target)
            : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
                => node == source ? target : base.VisitParameter(node);
        }
    }
}
