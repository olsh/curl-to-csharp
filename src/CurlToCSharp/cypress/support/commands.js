Cypress.Commands.add('convertAssert', () => {
    cy.get('#csharp').should('be.visible')
    cy.get('#errors').should('not.be.visible')
    cy.get('#warnings').should('not.be.visible')
})

Cypress.Commands.add('setConvertIntercept', () => {
    cy.intercept('/convert').as('convert')
})

Cypress.Commands.add('waitConvertResponse', () => {
    cy.wait('@convert')
})
