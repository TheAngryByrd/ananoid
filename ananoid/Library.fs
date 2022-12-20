﻿namespace MulberryLabs.Ananoid

#nowarn "9" (* unverifiable IL - see `Library.stackspan` function for details *)
#nowarn "42" (* inline IL -- see `Tagged.nanoid.tag` function for details *)
#nowarn "9999" (* we ignore our own rules! -- see `NanoId.TryDelay` for details *)

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text.RegularExpressions
open Microsoft.FSharp.Core


[<AutoOpen>]
module Library =
  open Microsoft.FSharp.NativeInterop

  [<Literal>]
  let Unreachable =
    "The program executed an instruction that was thought to be unreachable."

  let inline unreachable (dataKey, dataValue) =
    let failure = InvalidProgramException Unreachable
    failure.Data[ dataKey ] <- dataValue
    raise failure

  let inline outOfRange paramName =
    raise (ArgumentOutOfRangeException paramName)

  let inline stackspan<'T when 'T : unmanaged> size =
    Span<'T>(size |> NativePtr.stackalloc<'T> |> NativePtr.toVoidPtr, size)

  let inline (|Empty|_|) value =
    if String.IsNullOrWhiteSpace value then Some() else None

  let inline (|Trimmed|) (value : string) =
    Trimmed(if String.IsNullOrWhiteSpace value then "" else value.Trim())

  let inline (|Length|) (Trimmed trimmed) = Length(uint32 trimmed.Length)


type IAlphabet =
  abstract Letters : string
  abstract IncludesAll : value : string -> bool


module CharSets =
  [<Literal>]
  let UrlSafe =
    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz-"

  [<Literal>]
  let Numbers = "0123456789"

  [<Literal>]
  let HexadecimalLowercase = "0123456789abcdef"

  [<Literal>]
  let HexadecimalUppercase = "0123456789ABCDEF"

  [<Literal>]
  let Lowercase = "abcdefghijklmnopqrstuvwxyz"

  [<Literal>]
  let Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

  [<Literal>]
  let Alphanumeric =
    "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"

  [<Literal>]
  let NoLookalikes = "346789ABCDEFGHJKLMNPQRTUVWXYabcdefghijkmnpqrtwxyz"

  [<Literal>]
  let NoLookalikesSafe = "6789BCDFGHJKLMNPQRTWbcdfghjkmnpqrtwz"


module Patterns =
  [<Literal>]
  let UrlSafe = "^[a-zA-Z0-9_-]+$"

  [<Literal>]
  let Numbers = "^[0-9]+$"

  [<Literal>]
  let HexadecimalLowercase = "^[0-9abcdef]+$"

  [<Literal>]
  let HexadecimalUppercase = "^[0-9ABCDEF]+$"

  [<Literal>]
  let Lowercase = "^[a-z]+$"

  [<Literal>]
  let Uppercase = "^[A-Z]+$"

  [<Literal>]
  let Alphanumeric = "^[a-zA-Z0-9]+$"

  [<Literal>]
  let NoLookalikes = "^[346789ABCDEFGHJKLMNPQRTUVWXYabcdefghijkmnpqrtwxyz]+$"

  [<Literal>]
  let NoLookalikesSafe = "^[6789BCDFGHJKLMNPQRTWbcdfghjkmnpqrtwz]+$"


type AlphabetError =
  | AlphabetTooLarge
  | AlphabetTooSmall
  | IncoherentAlphabet
  | IncompatibleAlphabet
  member me.Message =
    match me with
    | AlphabetTooLarge -> "Alphabet may not contain more than 255 letters."
    | AlphabetTooSmall -> "Alphabet must contain at least one letter."
    | IncoherentAlphabet -> "Alphabet cannot validate its own letters."
    | IncompatibleAlphabet -> "Alphabet failed to validate given letters."
  override me.ToString() =
    let case =
      match me with
      | AlphabetTooLarge -> nameof AlphabetTooLarge
      | AlphabetTooSmall -> nameof AlphabetTooSmall
      | IncoherentAlphabet -> nameof IncoherentAlphabet
      | IncompatibleAlphabet -> nameof IncompatibleAlphabet
    $"{nameof AlphabetError}.{case} '{me.Message}'"


type Alphabet =
  | Alphanumeric
  | HexadecimalLowercase
  | HexadecimalUppercase
  | Lowercase
  | NoLookalikes
  | NoLookalikesSafe
  | Numbers
  | Uppercase
  | UrlSafe
  override me.ToString() = (me :> IAlphabet).Letters

  interface IAlphabet with
    member me.Letters =
      match me with
      | Alphanumeric -> CharSets.Alphanumeric
      | UrlSafe -> CharSets.UrlSafe
      | HexadecimalLowercase -> CharSets.HexadecimalLowercase
      | HexadecimalUppercase -> CharSets.HexadecimalUppercase
      | Lowercase -> CharSets.Lowercase
      | NoLookalikes -> CharSets.NoLookalikes
      | NoLookalikesSafe -> CharSets.NoLookalikesSafe
      | Numbers -> CharSets.Numbers
      | Uppercase -> CharSets.Uppercase
    member me.IncludesAll(value) =
      match value with
      | Empty -> true
      | Trimmed raw ->
        let spec =
          match me with
          | Alphanumeric -> Patterns.Alphanumeric
          | UrlSafe -> Patterns.UrlSafe
          | HexadecimalLowercase -> Patterns.HexadecimalLowercase
          | HexadecimalUppercase -> Patterns.HexadecimalUppercase
          | Lowercase -> Patterns.Lowercase
          | NoLookalikes -> Patterns.NoLookalikes
          | NoLookalikesSafe -> Patterns.NoLookalikesSafe
          | Numbers -> Patterns.Numbers
          | Uppercase -> Patterns.Uppercase
        Regex.IsMatch(raw, spec, RegexOptions.Compiled, TimeSpan.FromSeconds 1)

  static member Validate(alphabet : IAlphabet) =
    if isNull (alphabet :> obj) then
      Error AlphabetTooSmall
    elif alphabet.Letters.Length <= 0 then
      Error AlphabetTooSmall
    elif 256 <= alphabet.Letters.Length then
      Error AlphabetTooLarge
    elif not (alphabet.IncludesAll alphabet.Letters) then
      Error IncoherentAlphabet
    else
      Ok alphabet


[<NoComparison>]
type NanoIdOptions =
  {
    Alphabet' : IAlphabet
    Size' : int
  }
  member me.Alphabet = me.Alphabet'
  member me.Size = me.Size'

  member me.Resize(size) = { me with Size' = max 0 size }

  static member Of(alphabet : IAlphabet, size) =
    alphabet
    |> Alphabet.Validate
    |> Result.map (fun _ -> { Alphabet' = alphabet; Size' = max 0 size })

  static member UrlSafe = { Alphabet' = UrlSafe; Size' = 21 }

  static member Numbers = { NanoIdOptions.UrlSafe with Alphabet' = Numbers }

  static member HexadecimalLowercase =
    { NanoIdOptions.UrlSafe with Alphabet' = HexadecimalLowercase }

  static member HexadecimalUppercase =
    { NanoIdOptions.UrlSafe with Alphabet' = HexadecimalUppercase }

  static member Lowercase = { NanoIdOptions.UrlSafe with Alphabet' = Lowercase }

  static member Uppercase = { NanoIdOptions.UrlSafe with Alphabet' = Uppercase }

  static member Alphanumeric =
    { NanoIdOptions.UrlSafe with Alphabet' = Alphanumeric }

  static member NoLookalikes =
    { NanoIdOptions.UrlSafe with Alphabet' = NoLookalikes }

  static member NoLookalikesSafe =
    { NanoIdOptions.UrlSafe with Alphabet' = NoLookalikesSafe }


module Core =
  open System.Security.Cryptography

  open type System.Numerics.BitOperations
  open type NanoIdOptions

  [<CompiledName("NewNanoId")>]
  let nanoIdOf (alphabet & Length length) size =
    if size <= 0 then
      ""
    elif length <= 0u || 256u <= length then
      outOfRange (nameof alphabet)
    else
      let mask = (2 <<< 31 - LeadingZeroCount((length - 1u) ||| 1u)) - 1
      let step = int (ceil ((1.6 * float mask * float size) / float length))

      let nanoid = stackspan<char> size
      let mutable nanoidCount = 0

      let buffer = stackspan<byte> step
      let mutable bufferCount = 0

      while nanoidCount < size do
        RandomNumberGenerator.Fill(buffer)
        bufferCount <- 0

        while nanoidCount < size && bufferCount < step do
          let index = int buffer[bufferCount] &&& mask
          bufferCount <- bufferCount + 1

          if index < int length then
            nanoid[nanoidCount] <- alphabet[index]
            nanoidCount <- nanoidCount + 1

      nanoid.ToString()

  [<CompiledName("NewNanoId")>]
  let nanoId () = nanoIdOf UrlSafe.Alphabet.Letters UrlSafe.Size


  module Tagged =
    [<CompiledName("string@measurealias")>]
    [<MeasureAnnotatedAbbreviation>]
    type string<[<Measure>] 'Tag> = string

    [<CompiledName("nanoid@measure")>]
    [<Measure>]
    type nanoid =
      static member tag value = (# "" (value : string) : string<nanoid> #)

    let nanoIdOf' alphabet size = nanoid.tag (nanoIdOf alphabet size)

    let nanoId' () = nanoIdOf' UrlSafe.Alphabet.Letters UrlSafe.Size


[<IsReadOnly; Struct>]
type NanoId(value : string, length : uint32) =
  member _.Length = length

  override _.ToString() = let (Trimmed value') = value in value'

  static member IsEmpty(nanoId : NanoId) = (nanoId.Length = 0u)

  static member Empty = NanoId()

  static member NewId({ Alphabet' = alphabet; Size' = size }) =
    match Alphabet.Validate alphabet with
    | Ok a ->
      match Core.nanoIdOf a.Letters size with
      | Empty -> NanoId.Empty
      | Trimmed t & Length n -> NanoId(t, n)

    | Error reason -> unreachable (nameof AlphabetError, reason)

  static member NewId() = NanoId.NewId(NanoIdOptions.UrlSafe)

  static member Parse(value, alphabet : IAlphabet) =
    alphabet
    |> Alphabet.Validate
    |> Result.bind (fun a ->
      match value with
      | Empty when a.IncludesAll("") -> Ok NanoId.Empty
      | Trimmed t & Length n when a.IncludesAll(t) -> Ok(NanoId(t, n))
      | _ -> Error IncompatibleAlphabet
    )

  static member Parse(value) = NanoId.Parse(value, UrlSafe)

  static member TryParse(value, alphabet, [<Out>] nanoId : outref<_>) =
    let result = NanoId.Parse(value, alphabet)
    nanoId <- result |> Result.defaultValue NanoId.Empty
    Result.isOk result

  static member TryParse(value, [<Out>] nanoId : outref<_>) =
    NanoId.TryParse(value, UrlSafe, &nanoId)

  [<CompiledName("NanoId@Delay")>]
  static member Delay(alphabet) =
    alphabet
    |> Alphabet.Validate
    |> Result.map (fun a size ->
      match Core.nanoIdOf a.Letters size with
      | Empty -> NanoId.Empty
      | Trimmed t & Length n -> NanoId(t, n)
    )

  [<CompiledName("Delay")>]
  [<CompilerMessage("Not intended for use from F#", 9999, IsHidden = true)>]
  static member DelegateDelay(alphabet) =
    alphabet |> NanoId.Delay |> Result.map (fun fn -> Func<_, _> fn)

  [<CompilerMessage("Not intended for use from F#", 9999, IsHidden = true)>]
  static member TryDelay(alphabet, [<Out>] makeNanoId : outref<_>) =
    let result = NanoId.DelegateDelay(alphabet)
    makeNanoId <- result |> Result.defaultValue null
    Result.isOk result
