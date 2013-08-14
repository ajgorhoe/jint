﻿using System;
using System.Linq;
using Jint.Native;
using Jint.Native.Errors;
using Jint.Native.Function;
using Jint.Parser.Ast;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime
{
    public class ExpressionInterpreter
    {
        private readonly Engine _engine;

        public ExpressionInterpreter(Engine engine)
        {
            _engine = engine;
        }

        private object EvaluateExpression(Expression expression)
        {
            return _engine.EvaluateExpression(expression);
        }

        public object EvaluateConditionalExpression(ConditionalExpression conditionalExpression)
        {
            var test = _engine.EvaluateExpression(conditionalExpression.Test);
            var evaluate = TypeConverter.ToBoolean(test) ? conditionalExpression.Consequent : conditionalExpression.Alternate;
            
            return _engine.EvaluateExpression(evaluate);
        }

        public object EvaluateAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            object rval = _engine.GetValue(EvaluateExpression(assignmentExpression.Right));

            var lref = EvaluateExpression(assignmentExpression.Left) as Reference;

            if (assignmentExpression.Operator == "=")
            {
                if(lref != null && lref.IsStrict() && lref.GetBase() is EnvironmentRecord && (lref.GetReferencedName() == "eval" || lref.GetReferencedName() == "arguments"))
                {
                    throw new SyntaxError();
                }

                _engine.PutValue(lref, rval);
                return rval;
            }

            object lval = _engine.GetValue(lref);

            switch (assignmentExpression.Operator)
            {
                case "+=":
                    var lprim = TypeConverter.ToPrimitive(lval);
                    var rprim = TypeConverter.ToPrimitive(rval);
                    if (TypeConverter.GetType(lprim) == TypeCode.String ||
                        TypeConverter.GetType(rprim) == TypeCode.String)
                    {
                        lval = TypeConverter.ToString(lprim) + TypeConverter.ToString(rprim);
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;

                case "-=":
                    lval = TypeConverter.ToNumber(lval) + TypeConverter.ToNumber(rval);
                    break;

                case "*=":
                    if (lval == Undefined.Instance || rval == Undefined.Instance)
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) * TypeConverter.ToNumber(rval);
                    }
                    break;

                case "/=":
                    if (lval == Undefined.Instance || rval == Undefined.Instance)
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) / TypeConverter.ToNumber(rval);
                    }
                    break;

                case "%=":
                    if (lval == Undefined.Instance || rval == Undefined.Instance)
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) % TypeConverter.ToNumber(rval);
                    }
                    break;

                case "&=":
                    lval = TypeConverter.ToInt32(lval) & TypeConverter.ToInt32(rval);
                    break;

                case "|=":
                    lval = TypeConverter.ToInt32(lval) | TypeConverter.ToInt32(rval);
                    break;

                case "^=":
                    lval = TypeConverter.ToInt32(lval) ^ TypeConverter.ToInt32(rval);
                    break;

                case "<<=":
                    lval = TypeConverter.ToInt32(lval) << (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                case ">>>=":
                    lval = (uint)TypeConverter.ToInt32(lval) >> (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;
                
                default:
                    throw new NotImplementedException();

            }

            _engine.PutValue(lref, lval);

            return lval;
        }

        public object EvaluateBinaryExpression(BinaryExpression expression)
        {
            object left = _engine.GetValue(EvaluateExpression(expression.Left));
            object right = _engine.GetValue(EvaluateExpression(expression.Right));
            object value;

            switch (expression.Operator)
            {
                case "+":
                    var lprim = TypeConverter.ToPrimitive(left);
                    var rprim = TypeConverter.ToPrimitive(right);
                    if (TypeConverter.GetType(lprim) == TypeCode.String ||
                        TypeConverter.GetType(rprim) == TypeCode.String)
                    {
                        value = TypeConverter.ToString(lprim) + TypeConverter.ToString(rprim);
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;
                
                case "-":
                    value = TypeConverter.ToNumber(left) - TypeConverter.ToNumber(right);
                    break;
                
                case "*":
                    if (left == Undefined.Instance || right == Undefined.Instance)
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) * TypeConverter.ToNumber(right);
                    }
                    break;
                
                case "/":
                    if (left == Undefined.Instance || right == Undefined.Instance)
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) / TypeConverter.ToNumber(right);
                    }
                    break;

                case "%":
                    if (left == Undefined.Instance || right == Undefined.Instance)
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) % TypeConverter.ToNumber(right);
                    }
                    break;

                case "==":
                    value = left.Equals(right);
                    break;
                
                case "!=":
                    value = !left.Equals(right);
                    break;
                
                case ">":
                    value = TypeConverter.ToNumber(left) > TypeConverter.ToNumber(right);
                    break;

                case ">=":
                    value = TypeConverter.ToNumber(left) >= TypeConverter.ToNumber(right);
                    break;
                
                case "<":
                    value = TypeConverter.ToNumber(left) < TypeConverter.ToNumber(right);
                    break;
                
                case "<=":
                    value = TypeConverter.ToNumber(left) <= TypeConverter.ToNumber(right);
                    break;
                
                case "===":
                    return StriclyEqual(left, right);
                
                case "!==":
                    return !StriclyEqual(left, right);

                case "&":
                    return TypeConverter.ToInt32(left) & TypeConverter.ToInt32(right);

                case "|":
                    return TypeConverter.ToInt32(left) | TypeConverter.ToInt32(right);

                case "^":
                    return TypeConverter.ToInt32(left) ^ TypeConverter.ToInt32(right);

                case "<<":
                    return TypeConverter.ToInt32(left) << (int)(TypeConverter.ToUint32(right) & 0x1F);

                case ">>":
                    return TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);

                case ">>>":
                    return (uint)TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);

                case "instanceof":
                    var f = (FunctionInstance)right;
                    value = f.HasInstance(left);
                    break;
                
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public static bool StriclyEqual(object x, object y)
        {
            var typea = TypeConverter.GetType(x);
            var typeb = TypeConverter.GetType(y);

            if (typea != typeb)
            {
                return false;
            }    

            if (typea == TypeCode.Empty)
            {
                return true;
            }
            if (typea == TypeCode.Double)
            {
                var nx = TypeConverter.ToNumber(x);
                var ny = TypeConverter.ToNumber(y);
                if (double.IsNaN(nx) || double.IsNaN(ny))
                {
                    return false;
                }
                if (nx == ny)
                {
                    return true;
                }
                return false;
            }
            if (typea == TypeCode.String)
            {
                return TypeConverter.ToString(x) == TypeConverter.ToString(y);
            }
            if (typea == TypeCode.Boolean)
            {
                return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
            }
            return x == y;
        }

        public object EvaluateIdentifier(Identifier identifier)
        {
            return _engine.ExecutionContext.LexicalEnvironment.GetIdentifierReference(identifier.Name, _engine.Options.IsStrict());
        }

        public object EvaluateLiteral(Literal literal)
        {
            return literal.Value ?? Null.Instance;
        }

        public object EvaluateObjectExpression(ObjectExpression objectExpression)
        {
            var value = _engine.Object.Construct(Arguments.Empty);

            foreach (var property in objectExpression.Properties)
            {
                switch (property.Kind)
                {
                    case PropertyKind.Data:
                        value.DefineOwnProperty(property.Key.GetKey(), new DataDescriptor(property.Value), false);
                        break;
                    case PropertyKind.Get:
                        throw new NotImplementedException();
                        break;
                    case PropertyKind.Set:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return value;
        }

        public object EvaluateMemberExpression(MemberExpression memberExpression)
        {
            var baseValue = _engine.GetValue(EvaluateExpression(memberExpression.Object));

            string propertyName =
                !memberExpression.Computed
                    ? memberExpression.Property.As<Identifier>().Name // o.foo 
                    : EvaluateExpression(memberExpression.Property).ToString(); // o['foo']

            return new Reference(baseValue, propertyName, false);
        }

        public object EvaluateFunctionExpression(FunctionExpression functionExpression)
        {
            string identifier = functionExpression.Id != null ? functionExpression.Id.Name : null;
            return new ScriptFunctionInstance(
                _engine,
                functionExpression,
                _engine.Function.Prototype,
                _engine.Object.Construct(Arguments.Empty),
                LexicalEnvironment.NewDeclarativeEnvironment(_engine.ExecutionContext.LexicalEnvironment),
                functionExpression.Strict
                );
        }

        public object EvaluateCallExpression(CallExpression callExpression)
        {
            var callee = EvaluateExpression(callExpression.Callee);
            var func = _engine.GetValue(callee);
            object thisObject;

            // todo: implement as in http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.4
            var arguments = callExpression.Arguments.Select(EvaluateExpression).Select(_engine.GetValue).ToArray();

            if (TypeConverter.GetType(func) != TypeCode.Object)
            {
                throw new TypeError();
            }

            var callable = func as ICallable;
            if (callable == null)
            {
                throw new TypeError();
            }

            var r = callee as Reference;
            if (r != null)
            {
                if (r.IsPropertyReference())
                {
                    thisObject = r.GetBase();
                }
                else
                {
                    var env = r.GetBase() as EnvironmentRecord;
                    thisObject = env.ImplicitThisValue();
                }
            }
            else
            {
                thisObject = Undefined.Instance;
            }

            return callable.Call(thisObject, arguments);
        }

        public object EvaluateSequenceExpression(SequenceExpression sequenceExpression)
        {
            foreach (var expression in sequenceExpression.Expressions)
            {
                _engine.EvaluateExpression(expression);
            }

            return Undefined.Instance;
        }

        public object EvaluateUpdateExpression(UpdateExpression updateExpression)
        {
            var r = EvaluateExpression(updateExpression.Argument) as Reference;

            var value = _engine.GetValue(r);
            var old = value;

            switch (updateExpression.Operator)
            {
                case "++" :
                    value = TypeConverter.ToNumber(value) + 1;
                    break;
                case "--":
                    value = TypeConverter.ToNumber(value) - 1;
                    break;
                default:
                    throw new ArgumentException();
            }

            _engine.PutValue(r, value);

            return updateExpression.Prefix ? value : old;
        }

        public object EvaluateThisExpression(ThisExpression thisExpression)
        {
            return _engine.ExecutionContext.ThisBinding;
        }

        public object EvaluateNewExpression(NewExpression newExpression)
        {
            var arguments = newExpression.Arguments.Select(EvaluateExpression).ToArray();
            
            // todo: optimize by defining a common abstract class or interface
            var callee = (IConstructor)_engine.GetValue(EvaluateExpression(newExpression.Callee));

            // construct the new instance using the Function's constructor method
            var instance = callee.Construct(arguments);

            // initializes the new instance by executing the Function
            callee.Call(instance, arguments.ToArray());

            return instance;
        }

        public object EvaluateArrayExpression(ArrayExpression arrayExpression)
        {
            var arguments = arrayExpression.Elements.Select(EvaluateExpression).ToArray();

            // construct the new instance using the Function's constructor method
            var instance = _engine.Array.Construct(arguments);

            return instance;
        }

        public object EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            var value = _engine.EvaluateExpression(unaryExpression.Argument);
            Reference r;

            switch (unaryExpression.Operator)
            {
                case "++" :
                    r = value as Reference;
                    if(r != null 
                        && r.IsStrict() 
                        && (r.GetBase() is EnvironmentRecord )
                        && (Array.IndexOf(new []{"eval", "arguments"}, r.GetReferencedName()) != -1) )
                    {
                        throw new SyntaxError();
                    }

                    var oldValue = _engine.GetValue(value);
                    var newValue = TypeConverter.ToNumber(value) + 1;
                    _engine.PutValue(r, newValue);

                    return unaryExpression.Prefix ? newValue : oldValue;
                    
                case "--":
                    r = value as Reference;
                    if(r != null 
                        && r.IsStrict() 
                        && (r.GetBase() is EnvironmentRecord )
                        && (Array.IndexOf(new []{"eval", "arguments"}, r.GetReferencedName()) != -1) )
                    {
                        throw new SyntaxError();
                    }

                    oldValue = _engine.GetValue(value);
                    newValue = TypeConverter.ToNumber(value) - 1;
                    _engine.PutValue(r, newValue);

                    return unaryExpression.Prefix ? newValue : oldValue;
                    
                case "+":
                    return TypeConverter.ToNumber(_engine.GetValue(value));
                    
                case "-":
                    var n = TypeConverter.ToNumber(_engine.GetValue(value));
                    return double.IsNaN(n) ? double.NaN : n*-1;
                
                case "~":
                    return ~TypeConverter.ToInt32(_engine.GetValue(value));
                
                case "!":
                    return !TypeConverter.ToBoolean(_engine.GetValue(value));
                
                case "delete":
                    r = value as Reference;
                    if (r == null)
                    {
                        return true;
                    }
                    if (r.IsUnresolvableReference())
                    {
                        if (r.IsStrict())
                        {
                            throw new SyntaxError();
                        }

                        return true;
                    }
                    if (r.IsPropertyReference())
                    {
                        var o = TypeConverter.ToObject(_engine, r.GetBase());
                        return o.Delete(r.GetReferencedName(), r.IsStrict());
                    }
                    if (r.IsStrict())
                    {
                        throw new SyntaxError();
                    }
                    var bindings = r.GetBase() as EnvironmentRecord;
                    return bindings.DeleteBinding(r.GetReferencedName());
                
                case "void":
                    _engine.GetValue(value);
                    return Undefined.Instance;

                case "typeof":
                    r = value as Reference;
                    if (r != null)
                    {
                        if (r.IsUnresolvableReference())
                        {
                            return "undefined";
                        }
                    }
                    var v = _engine.GetValue(value);
                    if (v == Undefined.Instance)
                    {
                        return "undefined";
                    }
                    if (v == Null.Instance)
                    {
                        return "object";
                    }
                    switch (TypeConverter.GetType(v))
                    {
                        case TypeCode.Boolean: return "boolean";
                        case TypeCode.Double: return "number";
                        case TypeCode.String: return "string";
                    }
                    if (v is ICallable)
                    {
                        return "function";
                    }
                    return "object";

                default:
                    throw new ArgumentException();
            }
        }
    }
}
