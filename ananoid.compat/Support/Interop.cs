/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
namespace pblasucci.Ananoid.Compat.Support;

using Microsoft.FSharp.Core;


/// <summary>
/// Simplifies working with instances of <c>FSharpResult</c>.
/// </summary>
public static class FSharpResultExtensions
{
  /// <summary>
  /// Executes one of the given callbacks, based on the state of the
  /// <c>result</c>, passing any state-specific data into the callback
  /// (note: guaranteed to invoke exactly one of the two given callbacks).
  /// </summary>
  /// <param name="result">An <c>FSharpResult</c> with which to work.</param>
  /// <param name="okay">Invoked if the <c>result</c> is 'OK'.</param>
  /// <param name="error">Invoked if the <c>result</c>is in 'Error'.</param>
  /// <typeparam name="T">The type of data available when the <c>result</c> is 'OK'.</typeparam>
  /// <typeparam name="TError">The type of data available when the <c>result</c>is in error.</typeparam>
  /// <typeparam name="TReturn">The type of data returned from either callback.</typeparam>
  /// <returns>The result of calling one or the other callbacks.</returns>
  /// <exception cref="ArgumentNullException">Raised if either callback is <c>null</c>.</exception>
  public static TReturn? Match
    <T, TError, TReturn>(
      this FSharpResult<T, TError> result,
      Func<T, TReturn?> okay,
      Func<TError, TReturn?> error
    )
  {
    if (okay is null) throw new ArgumentNullException(nameof(okay));
    if (error is null) throw new ArgumentNullException(nameof(error));

    return result switch
    {
      { IsError: true, ErrorValue: var x } => error(x),
      { ResultValue: var value } => okay(value)
    };
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="result">An <c>FSharpResult</c> with which to work.</param>
  /// <param name="error"></param>
  /// <typeparam name="T">The type of data available when the <c>result</c> is 'OK'.</typeparam>
  /// <typeparam name="TError">The type of data available when the <c>result</c>is in error.</typeparam>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static T GetValueOrThrow<T, TError>(
    this FSharpResult<T, TError> result,
    Func<TError, Exception> error
  )
    => result switch
    {
      { IsError: true, ErrorValue: var x } => throw error(x),
      { ResultValue: var value } => value
    };

  /// <summary>
  ///
  /// </summary>
  /// <param name="result">An <c>FSharpResult</c> with which to work.</param>
  /// <param name="isOk"></param>
  /// <param name="value"></param>
  /// <param name="error"></param>
  /// <typeparam name="T">The type of data available when the <c>result</c> is 'OK'.</typeparam>
  /// <typeparam name="TError">The type of data available when the <c>result</c>is in error.</typeparam>
  public static void Deconstruct<T, TError>(
    this FSharpResult<T, TError> result,
    out bool isOk,
    out T? value,
    out TError? error
  )
  {
    isOk = result.IsOk;
    value = isOk ? result.ResultValue : default;
    error = isOk is false ? result.ErrorValue : default;
  }
}
