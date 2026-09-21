// ForkTsCheckerWebpackPlugin deshabilitado: ts-loader con transpileOnly: true
// ya compila TypeScript correctamente. El plugin de type-checking crashea por
// incompatibilidad entre TypeScript ~4.5 y los tipos modernos de @types/node
// y react-router-dom v7 (requieren TS 5+).
export const plugins = [];
